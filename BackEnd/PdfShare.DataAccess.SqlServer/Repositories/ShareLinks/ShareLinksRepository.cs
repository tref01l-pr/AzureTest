using Mapster;
using Microsoft.EntityFrameworkCore;
using PdfShare.DataAccess.SqlServer.Entities.ShareLinkEntities;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.DataAccess.SqlServer.Repositories.ShareLinks;

public sealed class ShareLinksRepository(ApplicationDbContext context, TypeAdapterConfig mapperConfig) : IShareLinksRepository
{
    public Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class =>
        context.ShareLinks.AsNoTracking()
            .Where(l => l.Id == id)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public Task<TProjectTo?> GetByTokenAsync<TProjectTo>(string token, CancellationToken ct = default) where TProjectTo : class =>
        context.ShareLinks.AsNoTracking()
            .Where(l => l.Token == token)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TProjectTo>> GetByFileAsync<TProjectTo>(Guid fileId, CancellationToken ct = default) =>
        await context.ShareLinks.AsNoTracking()
            .Where(l => l.FileId == fileId)
            .OrderByDescending(l => l.CreatedAt)
            .ProjectToType<TProjectTo>(mapperConfig)
            .ToListAsync(ct);

    public async Task CreateAsync(ShareLink link, CancellationToken ct = default)
    {
        context.ShareLinks.Add(link.Adapt<ShareLinkEntity>(mapperConfig));
        await context.SaveChangesAsync(ct);
    }

    public async Task ReplaceUsersAsync(Guid linkId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        await context.ShareLinkUsers.Where(x => x.ShareLinkId == linkId).ExecuteDeleteAsync(ct);
        context.ShareLinkUsers.AddRange(userIds.Select(userId => new ShareLinkUserEntity { ShareLinkId = linkId, UserId = userId }));
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> RevokeAsync(Guid linkId, CancellationToken ct = default) =>
        await context.ShareLinks
            .Where(l => l.Id == linkId && !l.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsRevoked, true), ct) > 0;

    public Task<bool> HasUserAsync(Guid linkId, Guid userId, CancellationToken ct = default) =>
        context.ShareLinkUsers.AnyAsync(x => x.ShareLinkId == linkId && x.UserId == userId, ct);
}
