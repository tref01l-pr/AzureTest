using PdfShare.API.Extensions;
using PdfShare.DataAccess.SqlServer;
using PdfShare.Infrastructure.Services;

namespace PdfShare.UnitTests.Infrastructure;

public class BlobStorageServiceTests
{
    [Fact]
    public void ContentDisposition_UnicodeName_HasAsciiFallbackAndRfc5987Name()
    {
        var header = BlobStorageService.BuildContentDisposition("Отчёт \"Q4\".pdf");

        header.Should().StartWith("attachment; filename=\"");
        header.Should().NotContain("\"Q4\"");
        header.Should().Contain("filename*=UTF-8''%D0%9E%D1%82%D1%87%D1%91%D1%82");
    }

    [Fact]
    public void PublicBaseUrl_ReplacesInternalDockerHostButKeepsPathAndSas()
    {
        var internalUri = new Uri("http://azurite:10000/devstoreaccount1/pdfs/a/b.pdf?sv=1&sig=abc");

        var rewritten = BlobStorageService.RewriteForPublicAccess(internalUri, new Uri("http://localhost:10000"));

        rewritten.ToString().Should().Be("http://localhost:10000/devstoreaccount1/pdfs/a/b.pdf?sv=1&sig=abc");
    }

    [Fact]
    public void NoPublicBaseUrl_KeepsUri()
    {
        var uri = new Uri("https://acct.blob.core.windows.net/pdfs/x.pdf?sig=1");

        BlobStorageService.RewriteForPublicAccess(uri, null).Should().BeSameAs(uri);
    }
}

public class ResultExtensionsTests
{
    [Theory]
    [InlineData("File.NotFound", 404)]
    [InlineData("ShareLink.Unauthorized", 401)]
    [InlineData("ShareLink.Forbidden", 403)]
    [InlineData("Auth.Conflict", 409)]
    [InlineData("ShareLink.Expired", 410)]
    [InlineData("Auth.LockedOut", 429)]
    [InlineData("File.InvalidType", 400)]
    public void ErrorCodeSuffix_MapsToHttpStatus(string code, int status) =>
        ResultExtensions.GetStatusCode(code).Should().Be(status);
}

public class MappingConfigTests
{
    [Fact]
    public void AllMappings_Compile() =>
        FluentActions.Invoking(() =>
        {
            var config = DependencyInjection.CreateMapperConfig();
            config.Compile();
            config.CompileProjection();
        }).Should().NotThrow();
}
