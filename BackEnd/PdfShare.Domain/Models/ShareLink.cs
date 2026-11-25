using CSharpFunctionalExtensions;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Errors;

namespace PdfShare.Domain.Models;

public sealed record ShareLink
{
    private ShareLink()
    {
    }

    public Guid Id { get; private init; }
    public Guid FileId { get; private init; }
    public string Token { get; private init; } = string.Empty;
    public ShareLinkType Type { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? ExpiresAt { get; private init; }
    public IReadOnlyCollection<Guid> UserIds { get; private init; } = [];

    public static Result<ShareLink> Create(
        Guid fileId,
        Guid ownerId,
        string token,
        ShareLinkType type,
        IEnumerable<Guid>? userIds,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
    {
        if (expiresAt is not null && expiresAt <= now)
        {
            return ShareLinkErrors.InvalidExpiration().ToResult<ShareLink>();
        }

        var usersResult = NormalizeUsers(type, ownerId, userIds);
        if (usersResult.IsFailure)
        {
            return Result.Failure<ShareLink>(usersResult.Error);
        }

        return new ShareLink
        {
            Id = Guid.NewGuid(),
            FileId = fileId,
            Token = token,
            Type = type,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            UserIds = usersResult.Value,
        };
    }

    public static Result<IReadOnlyCollection<Guid>> NormalizeUsers(ShareLinkType type, Guid ownerId, IEnumerable<Guid>? userIds)
    {
        var users = (userIds ?? [])
            .Where(id => id != Guid.Empty && id != ownerId)
            .Distinct()
            .ToArray();

        if (type == ShareLinkType.Public)
        {
            return users.Length == 0
                ? Result.Success<IReadOnlyCollection<Guid>>(users)
                : ShareLinkErrors.PublicCannotHaveUsers().ToResult<IReadOnlyCollection<Guid>>();
        }

        if (users.Length == 0)
        {
            return ShareLinkErrors.RestrictedRequiresUsers().ToResult<IReadOnlyCollection<Guid>>();
        }

        return users.Length > ShareLinkConstraints.MaxUsersPerLink
            ? ShareLinkErrors.TooManyUsers(ShareLinkConstraints.MaxUsersPerLink).ToResult<IReadOnlyCollection<Guid>>()
            : Result.Success<IReadOnlyCollection<Guid>>(users);
    }
}
