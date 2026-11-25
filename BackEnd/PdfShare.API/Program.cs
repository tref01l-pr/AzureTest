using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PdfShare.API.Configuration;
using PdfShare.API.DIExtensions;
using PdfShare.API.Middleware;
using PdfShare.Core;
using PdfShare.DataAccess.SqlServer;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Infrastructure;

namespace PdfShare.API;

public class Program
{
    public const string CorsPolicy = "Frontend";

    public static async Task Main(string[] args)
    {
        DotNetEnv.Env.TraversePath().NoClobber().Load();

        var builder = WebApplication.CreateBuilder(args);
        var settings = AppSettings.Load(builder.Configuration);

        if (builder.Configuration["API_PORT"] is { Length: > 0 } apiPort)
        {
            builder.WebHost.UseUrls($"http://localhost:{apiPort}");
        }

        builder.Services
            .AddCore()
            .AddDataAccess(settings.DatabaseUrl)
            .AddIdentityStores()
            .AddInfrastructure(settings.Storage)
            .AddJwtAuthentication(settings.Jwt)
            .AddAuthRateLimiting()
            .AddSwaggerWithJwt();

        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureValidationProblems();

        // Results.Ok/Created use the HTTP JSON options, not the MVC ones above.
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, policy =>
        {
            if (settings.CorsOrigins.Count > 0)
            {
                policy.WithOrigins([.. settings.CorsOrigins]).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            }
        }));

        var app = builder.Build();

        await InitializeAsync(app, settings);

        app.UseForwardedHeaders();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(CorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        await app.RunAsync();
    }

    private static async Task InitializeAsync(WebApplication app, AppSettings settings)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (settings.RunMigrations)
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }

        await scope.ServiceProvider.GetRequiredService<IBlobStorageService>().EnsureContainersAsync();
        await scope.ServiceProvider.GetRequiredService<IPdfProcessingQueue>().EnsureQueueAsync();
        await Seed.SeedDataAsync(scope.ServiceProvider, settings.SeedDemoUsers, settings.SeedDemoPassword, logger);
    }
}
