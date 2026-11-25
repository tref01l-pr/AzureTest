using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PdfShare.Core.Services.Files;
using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Files.Requests;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.UnitTests.Core;

public class FilesServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly Mock<IPdfFilesRepository> _files = new();
    private readonly Mock<IBlobStorageService> _blobs = new();
    private readonly Mock<IPdfProcessingQueue> _queue = new();
    private readonly FilesService _sut;

    public FilesServiceTests() =>
        _sut = new FilesService(_files.Object, _blobs.Object, _queue.Object, new FixedTimeProvider(TestData.Now), NullLogger<FilesService>.Instance);

    private static UploadPdfCommand Upload(byte[] content, string name = "doc.pdf", string type = "application/pdf") =>
        new(new MemoryStream(content), name, type, content.Length);

    [Fact]
    public async Task Upload_ValidPdf_StoresBlobSavesMetadataAndQueuesProcessing()
    {
        var result = await _sut.UploadAsync(Owner, Upload(TestData.MinimalPdf()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(FileStatus.Processing);
        _blobs.Verify(b => b.UploadAsync(StorageNames.PdfContainer, It.Is<string>(n => n.StartsWith($"{Owner:N}/")), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.CreateAsync(It.Is<PdfFile>(p => p.OwnerId == Owner && p.Id == result.Value.Id), It.IsAny<CancellationToken>()), Times.Once);
        _queue.Verify(q => q.EnqueueAsync(result.Value.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_PdfExtensionButNotPdfContent_IsRejectedBeforeStorage()
    {
        var result = await _sut.UploadAsync(Owner, Upload(Encoding.UTF8.GetBytes("MZ fake executable")));

        result.ErrorCode().Should().Be("File.InvalidType");
        _blobs.VerifyNoOtherCalls();
        _files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_MetadataSaveFails_DeletesOrphanBlobAndRethrows()
    {
        _files.Setup(f => f.CreateAsync(It.IsAny<PdfFile>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _sut.UploadAsync(Owner, Upload(TestData.MinimalPdf()));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _blobs.Verify(b => b.DeleteIfExistsAsync(StorageNames.PdfContainer, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_QueueUnavailable_UploadStillSucceeds()
    {
        _queue.Setup(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("queue down"));

        var result = await _sut.UploadAsync(Owner, Upload(TestData.MinimalPdf()));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Download_OtherUsersFile_IsNotFound()
    {
        var result = await _sut.GetDownloadUrlAsync(Owner, Guid.NewGuid());

        result.ErrorCode().Should().Be("File.NotFound");
    }

    [Fact]
    public async Task Download_OwnFile_ReturnsFiveMinuteAttachmentUrl()
    {
        var file = new PdfFileStorageDto { Id = Guid.NewGuid(), OwnerId = Owner, BlobName = "b.pdf", OriginalFileName = "Report.pdf", Status = FileStatus.Ready };
        _files.Setup(f => f.GetOwnedAsync<PdfFileStorageDto>(file.Id, Owner, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        _blobs.Setup(b => b.GetReadSasUriAsync(StorageNames.PdfContainer, "b.pdf", "Report.pdf", TestData.Now.AddMinutes(5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Uri("https://storage/pdfs/b.pdf?sig=x"));

        var result = await _sut.GetDownloadUrlAsync(Owner, file.Id);

        result.Value.ExpiresAt.Should().Be(TestData.Now.AddMinutes(5));
        result.Value.Url.Should().Be(new Uri("https://storage/pdfs/b.pdf?sig=x"));
    }

    [Fact]
    public async Task Delete_RemovesMetadataThenBlobs()
    {
        var file = new PdfFileStorageDto { Id = Guid.NewGuid(), OwnerId = Owner, BlobName = "b.pdf", ThumbnailBlobName = "b.png", Status = FileStatus.Ready };
        _files.Setup(f => f.GetOwnedAsync<PdfFileStorageDto>(file.Id, Owner, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        _files.Setup(f => f.DeleteAsync(file.Id, Owner, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(Owner, file.Id);

        result.IsSuccess.Should().BeTrue();
        _blobs.Verify(b => b.DeleteIfExistsAsync(StorageNames.PdfContainer, "b.pdf", It.IsAny<CancellationToken>()));
        _blobs.Verify(b => b.DeleteIfExistsAsync(StorageNames.ThumbnailContainer, "b.png", It.IsAny<CancellationToken>()));
    }
}
