using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Options;
using PdfShare.Infrastructure.Services;

namespace PdfShare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, BlobStorageOptions storage)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<BlobStorageOptions>().Configure(o =>
        {
            o.ConnectionString = storage.ConnectionString;
            o.BlobServiceUri = storage.BlobServiceUri;
            o.QueueServiceUri = storage.QueueServiceUri;
            o.PublicBaseUrl = storage.PublicBaseUrl;
        });

        // The Functions queue trigger decodes base64.
        var queueOptions = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };

        if (!string.IsNullOrWhiteSpace(storage.ConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(storage.ConnectionString));
            services.AddSingleton(new QueueServiceClient(storage.ConnectionString, queueOptions));
        }
        else
        {
            if (storage.BlobServiceUri is null || storage.QueueServiceUri is null)
            {
                throw new InvalidOperationException("Set BLOB_CONNECTION_STRING, or BLOB_SERVICE_URI and QUEUE_SERVICE_URI for managed identity.");
            }

            TokenCredential credential = new DefaultAzureCredential();
            services.AddSingleton(new BlobServiceClient(storage.BlobServiceUri, credential));
            services.AddSingleton(new QueueServiceClient(storage.QueueServiceUri, credential, queueOptions));
        }

        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IPdfProcessingQueue, PdfProcessingQueue>();
        return services;
    }
}
