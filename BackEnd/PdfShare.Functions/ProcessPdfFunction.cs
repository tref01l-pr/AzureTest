using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Models;
using PdfShare.Functions.Services;

namespace PdfShare.Functions;

public sealed class ProcessPdfFunction(
    IPdfFilesRepository filesRepository,
    IBlobStorageService blobStorage,
    IPdfAnalyzer analyzer,
    ILogger<ProcessPdfFunction> logger)
{
    public const string ThumbnailContentType = "image/png";
    public const string MissingContentReason = "The uploaded file content is missing.";

    [Function("ProcessPdf")]
    public async Task Run(
        [QueueTrigger(StorageNames.PdfUploadedQueue, Connection = "Storage")] PdfUploadedMessage message,
        CancellationToken ct)
    {
        var file = await filesRepository.GetByIdAsync<PdfFileStorageDto>(message.FileId, ct);
        if (file is null)
        {
            logger.LogInformation("File {FileId} no longer exists, skipping", message.FileId);
            return;
        }

        if (file.Status != FileStatus.Processing)
        {
            logger.LogInformation("File {FileId} already {Status}, skipping", file.Id, file.Status);
            return;
        }

        var pdf = await TryDownloadAsync(file, ct);
        if (pdf is null)
        {
            await filesRepository.MarkRejectedAsync(file.Id, MissingContentReason, ct);
            return;
        }

        var analysis = analyzer.Analyze(pdf);
        if (analysis.IsFailure)
        {
            if (await filesRepository.MarkRejectedAsync(file.Id, analysis.Error, ct))
            {
                await blobStorage.DeleteIfExistsAsync(StorageNames.PdfContainer, file.BlobName, ct);
            }

            logger.LogInformation("File {FileId} rejected: {Reason}", file.Id, analysis.Error);
            return;
        }

        var thumbnailBlobName = PdfFile.BuildThumbnailBlobName(file.OwnerId, file.Id);
        await using (var thumbnail = new MemoryStream(analysis.Value.ThumbnailPng))
        {
            await blobStorage.UploadAsync(StorageNames.ThumbnailContainer, thumbnailBlobName, thumbnail, ThumbnailContentType, ct);
        }

        if (!await filesRepository.MarkReadyAsync(file.Id, analysis.Value.PageCount, thumbnailBlobName, ct))
        {
            await blobStorage.DeleteIfExistsAsync(StorageNames.ThumbnailContainer, thumbnailBlobName, ct);
            return;
        }

        logger.LogInformation("File {FileId} is ready ({Pages} pages)", file.Id, analysis.Value.PageCount);
    }

    private async Task<byte[]?> TryDownloadAsync(PdfFileStorageDto file, CancellationToken ct)
    {
        try
        {
            await using var source = await blobStorage.OpenReadAsync(StorageNames.PdfContainer, file.BlobName, ct);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Blob {BlobName} for file {FileId} not found", file.BlobName, file.Id);
            return null;
        }
    }
}
