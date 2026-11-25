using Mapster;
using Microsoft.EntityFrameworkCore;
using PdfShare.DataAccess.SqlServer.Entities.SessionEntities;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.DataAccess.SqlServer.Repositories.Sessions;

public sealed class SessionsRepository(ApplicationDbContext context, TypeAdapterConfig mapperConfig) : ISessionsRepository
{
    public Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class =>
        context.Sessions.AsNoTracking()
            .Where(s => s.Id == id)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public Task<TProjectTo?> GetByRefreshTokenHashAsync<TProjectTo>(string refreshTokenHash, CancellationToken ct = default) where TProjectTo : class =>
        context.Sessions.AsNoTracking()
            .Where(s => s.RefreshTokenHash == refreshTokenHash)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(Guid id, DateTimeOffset now, CancellationToken ct = default) =>
        context.Sessions.AnyAsync(s => s.Id == id && s.ExpirationDate > now, ct);

    public async Task CreateAsync(Session session, CancellationToken ct = default)
    {
        context.Sessions.Add(session.Adapt<SessionEntity>(mapperConfig));
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> RotateRefreshTokenAsync(Guid id, string newRefreshTokenHash, DateTimeOffset newExpirationDate, CancellationToken ct = default) =>
        await context.Sessions
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.RefreshTokenHash, newRefreshTokenHash)
                .SetProperty(x => x.ExpirationDate, newExpirationDate), ct) > 0;

    public Task DeleteByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default) =>
        context.Sessions.Where(s => s.RefreshTokenHash == refreshTokenHash).ExecuteDeleteAsync(ct);

    public Task DeleteByUserIdAsync(Guid userId, Guid? exceptSessionId = null, CancellationToken ct = default) =>
        context.Sessions
            .Where(s => s.UserId == userId && (exceptSessionId == null || s.Id != exceptSessionId))
            .ExecuteDeleteAsync(ct);
}
