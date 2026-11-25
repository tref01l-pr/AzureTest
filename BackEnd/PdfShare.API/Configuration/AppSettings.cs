using PdfShare.Domain.Options;

namespace PdfShare.API.Configuration;

public sealed class AppSettings
{
    public const int MinJwtSecretLength = 32;

    public required string DatabaseUrl { get; init; }
    public required JWTSecretOptions Jwt { get; init; }
    public required BlobStorageOptions Storage { get; init; }
    public required IReadOnlyList<string> CorsOrigins { get; init; }
    public bool RunMigrations { get; init; }
    public bool SeedDemoUsers { get; init; }
    public string? SeedDemoPassword { get; init; }

    public static AppSettings Load(IConfiguration config)
    {
        var jwtSecret = Required(config, "JWT_SECRET");
        if (jwtSecret.Length < MinJwtSecretLength)
        {
            throw new InvalidOperationException($"JWT_SECRET must be at least {MinJwtSecretLength} characters (HS256 needs a 256-bit key).");
        }

        return new AppSettings
        {
            DatabaseUrl = Required(config, "DATABASE_URL"),
            Jwt = new JWTSecretOptions
            {
                Secret = jwtSecret,
                AccessTokenLifetime = TimeSpan.FromMinutes(config.GetValue("ACCESS_TOKEN_MINUTES", 15)),
                RefreshTokenLifetime = TimeSpan.FromDays(config.GetValue("REFRESH_TOKEN_DAYS", 30)),
            },
            Storage = new BlobStorageOptions
            {
                ConnectionString = Optional(config, "BLOB_CONNECTION_STRING"),
                BlobServiceUri = OptionalUri(config, "BLOB_SERVICE_URI"),
                QueueServiceUri = OptionalUri(config, "QUEUE_SERVICE_URI"),
                PublicBaseUrl = OptionalUri(config, "BLOB_PUBLIC_BASE_URL"),
            },
            CorsOrigins = (Optional(config, "CORS_ORIGINS") ?? string.Empty)
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            RunMigrations = config.GetValue("RUN_MIGRATIONS", true),
            SeedDemoUsers = config.GetValue("SEED_DEMO_USERS", false),
            SeedDemoPassword = Optional(config, "SEED_DEMO_PASSWORD"),
        };
    }

    private static string Required(IConfiguration config, string key) =>
        Optional(config, key) ?? throw new InvalidOperationException($"Environment variable {key} is required. See .env.example.");

    private static string? Optional(IConfiguration config, string key) =>
        string.IsNullOrWhiteSpace(config[key]) ? null : config[key];

    private static Uri? OptionalUri(IConfiguration config, string key) =>
        Optional(config, key) is { } value ? new Uri(value, UriKind.Absolute) : null;
}
