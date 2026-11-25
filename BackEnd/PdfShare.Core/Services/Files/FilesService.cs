using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Common;
using PdfShare.Domain.DTO.Files.Requests;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.CoreInterfaces;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.Core.Services.Files;

public sealed class FilesService(
    IPdfFilesRepository filesRepository,
    IBlobStorageService blobStorage,
    IPdfProcessingQueue processingQueue,
    TimeProvider timeProvider,
    ILogger<FilesService> logger) : IFilesService
{
    public const int MaxPageSize = 100;

    public async Task<Result<PdfFileDto>> UploadAsync(Guid ownerId, UploadPdfCommand command, CancellationToken ct = default)
    {
        var created = PdfFile.Create(ownerId, command.FileName, command.ContentType, command.Length, timeProvider.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<PdfFileDto>(created.Error);
        }

        var file = created.Value;
        await using var content = await EnsureSeekableAsync(command.Content, ct);

        if (!await HasPdfSignatureAsync(content, ct))
        {
            return FileErrors.InvalidType().ToResult<PdfFileDto>();
        }

        await blobStorage.UploadAsync(StorageNames.PdfContainer, file.BlobName, content, FileConstraints.PdfContentType, ct);

        try
        {
            await filesRepository.CreateAsync(file, ct);
        }
        catch
        {
            await blobStorage.DeleteIfExistsAsync(StorageNames.PdfContainer, file.BlobName, CancellationToken.None);
            throw;
        }

        try
        {
            await processingQueue.EnqueueAsync(file.Id, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue file {FileId} for processing", file.Id);
        }

        return new PdfFileDto
        {
            Id = file.Id,
            OriginalFileName = file.OriginalFileName,
            SizeBytes = file.SizeBytes,
            Status = file.Status,
            UploadedAt = file.UploadedAt,
        };
    }

    public Task<PagedResult<PdfFileDto>> GetMyFilesAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default) =>
        filesRepository.GetByOwnerAsync<PdfFileDto>(ownerId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

    public Task<IReadOnlyList<SharedFileDto>> GetSharedWithMeAsync(Guid userId, CancellationToken ct = default) =>
        filesRepository.GetSharedWithUserAsync(userId, timeProvider.GetUtcNow(), ct);

    public async Task<Result<DownloadUrlDto>> GetDownloadUrlAsync(Guid ownerId, Guid fileId, CancellationToken ct = default)
    {
        var file = await filesRepository.GetOwnedAsync<PdfFileStorageDto>(fileId, ownerId, ct);
        if (file is null)
        {
            return FileErrors.NotFound().ToResult<DownloadUrlDto>();
        }

        if (file.Status == FileStatus.Rejected)
        {
            return FileErrors.NotReady().ToResult<DownloadUrlDto>();
        }

        return await CreateSasAsync(StorageNames.PdfContainer, file.BlobName, file.OriginalFileName, ct);
    }

    public async Task<Result<DownloadUrlDto>> GetThumbnailUrlAsync(Guid ownerId, Guid fileId, CancellationToken ct = default)
    {
        var file = await filesRepository.GetOwnedAsync<PdfFileStorageDto>(fileId, ownerId, ct);
        if (file is null)
        {
            return FileErrors.NotFound().ToResult<DownloadUrlDto>();
        }

        return file.ThumbnailBlobName is null
            ? FileErrors.NoThumbnail().ToResult<DownloadUrlDto>()
            : await CreateSasAsync(StorageNames.ThumbnailContainer, file.ThumbnailBlobName, downloadFileName: null, ct);
    }

    public async Task<Result> DeleteAsync(Guid ownerId, Guid fileId, CancellationToken ct = default)
    {
        var file = await filesRepository.GetOwnedAsync<PdfFileStorageDto>(fileId, ownerId, ct);
        if (file is null || !await filesRepository.DeleteAsync(fileId, ownerId, ct))
        {
            return FileErrors.NotFound();
        }

        await blobStorage.DeleteIfExistsAsync(StorageNames.PdfContainer, file.BlobName, ct);
        if (file.ThumbnailBlobName is not null)
        {
            await blobStorage.DeleteIfExistsAsync(StorageNames.ThumbnailContainer, file.ThumbnailBlobName, ct);
        }

        return Result.Success();
    }

    private async Task<DownloadUrlDto> CreateSasAsync(string container, string blobName, string? downloadFileName, CancellationToken ct)
    {
        var expiresAt = timeProvider.GetUtcNow().Add(ShareLinkConstraints.DownloadUrlLifetime);
        var url = await blobStorage.GetReadSasUriAsync(container, blobName, downloadFileName, expiresAt, ct);
        return new DownloadUrlDto(url, expiresAt);
    }

    private static async Task<Stream> EnsureSeekableAsync(Stream content, CancellationToken ct)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return content;
        }

        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task<bool> HasPdfSignatureAsync(Stream content, CancellationToken ct)
    {
        var header = new byte[FileConstraints.PdfSignature.Length];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        content.Position = 0;
        return read == header.Length && header.AsSpan().SequenceEqual(FileConstraints.PdfSignature);
    }
}
