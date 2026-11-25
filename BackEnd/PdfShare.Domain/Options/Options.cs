namespace PdfShare.Domain.Options;

public sealed class JWTSecretOptions
{
    public const string JWTSecret = "JWT_SECRET";

    public string Secret { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}

public sealed class BlobStorageOptions
{
    public string? ConnectionString { get; set; }

    public Uri? BlobServiceUri { get; set; }

    public Uri? QueueServiceUri { get; set; }

    public Uri? PublicBaseUrl { get; set; }
}
