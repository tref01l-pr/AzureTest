using Mapster;
using Microsoft.EntityFrameworkCore;
using PdfShare.DataAccess.SqlServer.Entities.FileEntities;
using PdfShare.Domain;
using PdfShare.Domain.DTO.Common;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.DataAccess.SqlServer.Repositories.Files;

public sealed class PdfFilesRepository(ApplicationDbContext context, TypeAdapterConfig mapperConfig) : IPdfFilesRepository
{
    public Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class =>
        context.PdfFiles.AsNoTracking()
            .Where(f => f.Id == id)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public Task<TProjectTo?> GetOwnedAsync<TProjectTo>(Guid id, Guid ownerId, CancellationToken ct = default) where TProjectTo : class =>
        context.PdfFiles.AsNoTracking()
            .Where(f => f.Id == id && f.OwnerId == ownerId)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<TProjectTo>> GetByOwnerAsync<TProjectTo>(Guid ownerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.PdfFiles.AsNoTracking().Where(f => f.OwnerId == ownerId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<TProjectTo>(mapperConfig)
            .ToListAsync(ct);

        return new PagedResult<TProjectTo> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<IReadOnlyList<SharedFileDto>> GetSharedWithUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default) =>
        await context.ShareLinkUsers.AsNoTracking()
            .Where(lu => lu.UserId == userId
                         && !lu.ShareLink.IsRevoked
                         && (lu.ShareLink.ExpiresAt == null || lu.ShareLink.ExpiresAt > now)
                         && lu.ShareLink.File.Status == FileStatus.Ready
                         && lu.ShareLink.File.OwnerId != userId)
            .OrderByDescending(lu => lu.ShareLink.CreatedAt)
            .Select(lu => new SharedFileDto
            {
                FileId = lu.ShareLink.FileId,
                OriginalFileName = lu.ShareLink.File.OriginalFileName,
                SizeBytes = lu.ShareLink.File.SizeBytes,
                PageCount = lu.ShareLink.File.PageCount,
                OwnerEmail = lu.ShareLink.File.Owner.Email!,
                Token = lu.ShareLink.Token,
                SharedAt = lu.ShareLink.CreatedAt,
                ExpiresAt = lu.ShareLink.ExpiresAt,
            })
            .ToListAsync(ct);

    public async Task CreateAsync(PdfFile file, CancellationToken ct = default)
    {
        context.PdfFiles.Add(file.Adapt<PdfFileEntity>(mapperConfig));
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkReadyAsync(Guid id, int pageCount, string? thumbnailBlobName, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = await context.PdfFiles
            .Where(f => f.Id == id && f.Status == FileStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Status, FileStatus.Ready)
                .SetProperty(f => f.PageCount, pageCount)
                .SetProperty(f => f.ThumbnailBlobName, thumbnailBlobName)
                .SetProperty(f => f.ProcessedAt, now), ct);
        return updated > 0;
    }

    public async Task<bool> MarkRejectedAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var safeReason = reason.Length > 500 ? reason[..500] : reason;
        var updated = await context.PdfFiles
            .Where(f => f.Id == id && f.Status == FileStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Status, FileStatus.Rejected)
                .SetProperty(f => f.RejectReason, safeReason)
                .SetProperty(f => f.ProcessedAt, now), ct);
        return updated > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var deleted = await context.PdfFiles
            .Where(f => f.Id == id && f.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
