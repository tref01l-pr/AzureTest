namespace PdfShare.Domain.Interfaces.InfrastructureInterfaces;

public interface IBlobStorageService
{
    Task EnsureContainersAsync(CancellationToken ct = default);
    Task UploadAsync(string container, string blobName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string container, string blobName, CancellationToken ct = default);
    Task DeleteIfExistsAsync(string container, string blobName, CancellationToken ct = default);

    Task<Uri> GetReadSasUriAsync(string container, string blobName, string? downloadFileName, DateTimeOffset expiresAt, CancellationToken ct = default);
}

public interface IPdfProcessingQueue
{
    Task EnsureQueueAsync(CancellationToken ct = default);
    Task EnqueueAsync(Guid fileId, CancellationToken ct = default);
}

public sealed record PdfUploadedMessage(Guid FileId);
