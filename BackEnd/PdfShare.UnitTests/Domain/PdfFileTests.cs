using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Models;

namespace PdfShare.UnitTests.Domain;

public class PdfFileTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Create_ValidPdf_StartsProcessingWithOwnerScopedBlobName()
    {
        var result = PdfFile.Create(Owner, "report.pdf", "application/pdf", 1024, TestData.Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(FileStatus.Processing);
        result.Value.OwnerId.Should().Be(Owner);
        result.Value.BlobName.Should().Be($"{Owner:N}/{result.Value.Id:N}.pdf");
    }

    [Theory]
    [InlineData("report.pdf", "application/pdf", 0, "File.Empty")]
    [InlineData("report.pdf", "application/pdf", FileConstraints.MaxFileSizeBytes + 1, "File.TooLarge")]
    [InlineData("report.docx", "application/pdf", 100, "File.InvalidType")]
    [InlineData("report.pdf", "text/plain", 100, "File.InvalidType")]
    [InlineData("   ", "application/pdf", 100, "File.InvalidName")]
    [InlineData(".pdf", "application/pdf", 100, "File.InvalidName")]
    public void Create_InvalidInput_Fails(string name, string contentType, long size, string expectedCode)
    {
        var result = PdfFile.Create(Owner, name, contentType, size, TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode().Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("APPLICATION/PDF")]
    public void Create_PdfWithOsProvidedContentType_IsAccepted(string? contentType) =>
        PdfFile.Create(Owner, "scan.pdf", contentType, 10, TestData.Now).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData("../../etc/evil.pdf", "evil.pdf")]
    [InlineData("C:\\Users\\me\\Desktop\\cv.pdf", "cv.pdf")]
    [InlineData("  spaced.PDF  ", "spaced.PDF")]
    [InlineData("quo\"te.pdf", "quote.pdf")]
    public void Create_StripsPathsAndUnsafeCharacters(string name, string expected)
    {
        PdfFile.Create(Owner, name, "application/pdf", 10, TestData.Now).Value.OriginalFileName.Should().Be(expected);
    }

    [Fact]
    public void Create_LongName_IsTruncatedKeepingExtension()
    {
        var name = new string('a', 400) + ".pdf";

        var stored = PdfFile.Create(Owner, name, "application/pdf", 10, TestData.Now).Value.OriginalFileName;

        stored.Length.Should().Be(FileConstraints.MaxFileNameLength);
        stored.Should().EndWith(".pdf");
    }
}
