using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Options;

namespace PdfShare.Infrastructure.Services;

public sealed class BlobStorageService(
    BlobServiceClient serviceClient,
    IOptions<BlobStorageOptions> options,
    TimeProvider timeProvider) : IBlobStorageService, IDisposable
{
    private static readonly TimeSpan DelegationKeyLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _keyLock = new(1, 1);
    private UserDelegationKey? _delegationKey;

    public async Task EnsureContainersAsync(CancellationToken ct = default)
    {
        foreach (var container in new[] { StorageNames.PdfContainer, StorageNames.ThumbnailContainer })
        {
            await serviceClient.GetBlobContainerClient(container).CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
    }

    public async Task UploadAsync(string container, string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var blob = serviceClient.GetBlobContainerClient(container).GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, ct);
    }

    public async Task<Stream> OpenReadAsync(string container, string blobName, CancellationToken ct = default)
    {
        var blob = serviceClient.GetBlobContainerClient(container).GetBlobClient(blobName);
        return await blob.OpenReadAsync(cancellationToken: ct);
    }

    public Task DeleteIfExistsAsync(string container, string blobName, CancellationToken ct = default) =>
        serviceClient.GetBlobContainerClient(container).GetBlobClient(blobName)
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);

    public async Task<Uri> GetReadSasUriAsync(string container, string blobName, string? downloadFileName, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var blob = serviceClient.GetBlobContainerClient(container).GetBlobClient(blobName);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
        {
            BlobContainerName = container,
            BlobName = blobName,
            Resource = "b",
            StartsOn = timeProvider.GetUtcNow().Subtract(ClockSkew),
            ContentDisposition = downloadFileName is null ? null : BuildContentDisposition(downloadFileName),
        };

        Uri uri;
        if (blob.CanGenerateSasUri)
        {
            uri = blob.GenerateSasUri(sas);
        }
        else
        {
            var key = await GetDelegationKeyAsync(expiresAt, ct);
            var builder = new BlobUriBuilder(blob.Uri) { Sas = sas.ToSasQueryParameters(key, serviceClient.AccountName) };
            uri = builder.ToUri();
        }

        return RewriteForPublicAccess(uri, options.Value.PublicBaseUrl);
    }

    public void Dispose() => _keyLock.Dispose();

    internal static string BuildContentDisposition(string fileName)
    {
        var ascii = new StringBuilder(fileName.Length);
        foreach (var c in fileName)
        {
            ascii.Append(c is >= ' ' and <= '~' and not '"' and not '\\' ? c : '_');
        }

        return $"attachment; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
    }

    internal static Uri RewriteForPublicAccess(Uri uri, Uri? publicBaseUrl)
    {
        if (publicBaseUrl is null)
        {
            return uri;
        }

        return new UriBuilder(uri)
        {
            Scheme = publicBaseUrl.Scheme,
            Host = publicBaseUrl.Host,
            Port = publicBaseUrl.IsDefaultPort ? -1 : publicBaseUrl.Port,
        }.Uri;
    }

    private async Task<UserDelegationKey> GetDelegationKeyAsync(DateTimeOffset sasExpiresAt, CancellationToken ct)
    {
        await _keyLock.WaitAsync(ct);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_delegationKey is null || _delegationKey.SignedExpiresOn < sasExpiresAt)
            {
                var keyExpiresAt = now.Add(DelegationKeyLifetime) > sasExpiresAt ? now.Add(DelegationKeyLifetime) : sasExpiresAt;
                _delegationKey = (await serviceClient.GetUserDelegationKeyAsync(now.Subtract(ClockSkew), keyExpiresAt, ct)).Value;
            }

            return _delegationKey;
        }
        finally
        {
            _keyLock.Release();
        }
    }
}
