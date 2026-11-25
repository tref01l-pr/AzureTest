namespace PdfShare.Domain.Models;

public sealed record Session
{
    private Session()
    {
    }

    public Guid Id { get; private init; }
    public Guid UserId { get; private init; }
    public string RefreshTokenHash { get; private init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset ExpirationDate { get; private init; }

    public static Session Create(Guid userId, string refreshTokenHash, DateTimeOffset now, TimeSpan lifetime) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        RefreshTokenHash = refreshTokenHash,
        CreatedAt = now,
        ExpirationDate = now.Add(lifetime),
    };
}
