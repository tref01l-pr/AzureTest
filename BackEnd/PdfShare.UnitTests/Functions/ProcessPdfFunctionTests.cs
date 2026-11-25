using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Functions;
using PdfShare.Functions.Services;

namespace PdfShare.UnitTests.Functions;

public class ProcessPdfFunctionTests
{
    private readonly Mock<IPdfFilesRepository> _files = new();
    private readonly Mock<IBlobStorageService> _blobs = new();
    private readonly Mock<IPdfAnalyzer> _analyzer = new();
    private readonly ProcessPdfFunction _sut;
    private readonly PdfFileStorageDto _file = new()
    {
        Id = Guid.NewGuid(),
        OwnerId = Guid.NewGuid(),
        BlobName = "owner/file.pdf",
        Status = FileStatus.Processing,
    };

    public ProcessPdfFunctionTests()
    {
        _sut = new ProcessPdfFunction(_files.Object, _blobs.Object, _analyzer.Object, NullLogger<ProcessPdfFunction>.Instance);
        _files.Setup(f => f.GetByIdAsync<PdfFileStorageDto>(_file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => _file);
        _blobs.Setup(b => b.OpenReadAsync(StorageNames.PdfContainer, _file.BlobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(TestData.MinimalPdf()));
    }

    [Fact]
    public async Task ValidPdf_UploadsThumbnailAndMarksReady()
    {
        _analyzer.Setup(a => a.Analyze(It.IsAny<byte[]>())).Returns(Result.Success(new PdfAnalysis(3, [1, 2, 3])));
        _files.Setup(f => f.MarkReadyAsync(_file.Id, 3, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.Run(new PdfUploadedMessage(_file.Id), CancellationToken.None);

        _blobs.Verify(b => b.UploadAsync(StorageNames.ThumbnailContainer, $"{_file.OwnerId:N}/{_file.Id:N}.png", It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()));
        _files.Verify(f => f.MarkReadyAsync(_file.Id, 3, $"{_file.OwnerId:N}/{_file.Id:N}.png", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task InvalidPdf_IsRejectedAndBlobDeleted()
    {
        _analyzer.Setup(a => a.Analyze(It.IsAny<byte[]>())).Returns(Result.Failure<PdfAnalysis>("broken"));
        _files.Setup(f => f.MarkRejectedAsync(_file.Id, "broken", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.Run(new PdfUploadedMessage(_file.Id), CancellationToken.None);

        _blobs.Verify(b => b.DeleteIfExistsAsync(StorageNames.PdfContainer, _file.BlobName, It.IsAny<CancellationToken>()));
        _files.Verify(f => f.MarkReadyAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RedeliveredMessage_ForProcessedFile_IsNoOp()
    {
        var processed = new PdfFileStorageDto { Id = _file.Id, OwnerId = _file.OwnerId, BlobName = _file.BlobName, Status = FileStatus.Ready };
        _files.Setup(f => f.GetByIdAsync<PdfFileStorageDto>(_file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(processed);

        await _sut.Run(new PdfUploadedMessage(_file.Id), CancellationToken.None);

        _analyzer.VerifyNoOtherCalls();
        _blobs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FileDeletedDuringProcessing_ThumbnailIsCleanedUp()
    {
        _analyzer.Setup(a => a.Analyze(It.IsAny<byte[]>())).Returns(Result.Success(new PdfAnalysis(1, [1])));
        _files.Setup(f => f.MarkReadyAsync(_file.Id, 1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.Run(new PdfUploadedMessage(_file.Id), CancellationToken.None);

        _blobs.Verify(b => b.DeleteIfExistsAsync(StorageNames.ThumbnailContainer, $"{_file.OwnerId:N}/{_file.Id:N}.png", It.IsAny<CancellationToken>()));
    }
}

public class PdfAnalyzerTests
{
    private readonly PdfAnalyzer _sut = new(NullLogger<PdfAnalyzer>.Instance);

    [Fact]
    public void ValidPdf_ReturnsPageCountAndPngThumbnail()
    {
        var result = _sut.Analyze(TestData.MinimalPdf());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error : null);
        result.Value.PageCount.Should().Be(1);
        result.Value.ThumbnailPng.Take(4).Should().Equal(0x89, (byte)'P', (byte)'N', (byte)'G');
    }

    [Fact]
    public void GarbageWithPdfHeader_IsRejected()
    {
        var result = _sut.Analyze(Encoding.ASCII.GetBytes("%PDF-1.7 this is not really a pdf"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PdfAnalyzer.InvalidPdfReason);
    }
}
