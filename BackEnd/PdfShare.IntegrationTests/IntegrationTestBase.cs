using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PdfShare.DataAccess.SqlServer;
using PdfShare.Domain;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.IntegrationTests;

public abstract class IntegrationTestBase
{
    private ServiceProvider _root = null!;
    private string _connectionString = null!;

    protected AsyncServiceScope Scope { get; private set; }
    protected T Get<T>() where T : notnull => Scope.ServiceProvider.GetRequiredService<T>();

    [OneTimeSetUp]
    public async Task CreateDatabaseAsync()
    {
        var server = Environment.GetEnvironmentVariable("INTEGRATION_SQL_SERVER")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";
        _connectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"PdfShare_Tests_{Guid.NewGuid():N}",
            MultipleActiveResultSets = true,
        }.ConnectionString;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(TimeProvider.System);
        services.AddDataAccess(_connectionString).AddIdentityStores();
        _root = services.BuildServiceProvider();

        await using var scope = _root.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }
        catch (SqlException ex)
        {
            Assert.Ignore($"SQL Server is not available for integration tests: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public async Task DropDatabaseAsync()
    {
        await using (var scope = _root.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureDeletedAsync();
        }

        await _root.DisposeAsync();
    }

    [SetUp]
    public void CreateScope() => Scope = _root.CreateAsyncScope();

    [TearDown]
    public async Task DisposeScopeAsync() => await Scope.DisposeAsync();

    protected async Task<UserCredentialsDto> CreateUserAsync(string? email = null)
    {
        var result = await Get<IIdentityService>().RegisterAsync(email ?? $"{Guid.NewGuid():N}@test.local", "Passw0rd!", Roles.User);
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error : null);
        return result.Value;
    }

    protected async Task<PdfFile> CreateFileAsync(Guid ownerId, bool ready = true)
    {
        var file = PdfFile.Create(ownerId, "doc.pdf", "application/pdf", 1234, DateTimeOffset.UtcNow).Value;
        var repository = Get<IPdfFilesRepository>();
        await repository.CreateAsync(file);
        if (ready)
        {
            await repository.MarkReadyAsync(file.Id, 2, null);
        }

        return file;
    }
}
