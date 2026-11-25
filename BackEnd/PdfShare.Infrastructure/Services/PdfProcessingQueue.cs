using System.Text.Json;
using Azure.Storage.Queues;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;

namespace PdfShare.Infrastructure.Services;

public sealed class PdfProcessingQueue(QueueServiceClient queueServiceClient) : IPdfProcessingQueue
{
    private readonly QueueClient _queue = queueServiceClient.GetQueueClient(StorageNames.PdfUploadedQueue);

    public Task EnsureQueueAsync(CancellationToken ct = default) => _queue.CreateIfNotExistsAsync(cancellationToken: ct);

    public Task EnqueueAsync(Guid fileId, CancellationToken ct = default) =>
        _queue.SendMessageAsync(JsonSerializer.Serialize(new PdfUploadedMessage(fileId)), ct);
}
