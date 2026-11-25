using Mapster;
using Microsoft.EntityFrameworkCore;
using PdfShare.Domain.DTO.Users;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.DataAccess.SqlServer.Repositories.Users;

public sealed class UsersRepository(ApplicationDbContext context, TypeAdapterConfig mapperConfig) : IUsersRepository
{
    public Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class =>
        context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .ProjectToType<TProjectTo>(mapperConfig)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserDto>> SearchByEmailAsync(string emailFragment, Guid excludeUserId, int take, CancellationToken ct = default)
    {
        var normalized = emailFragment.Trim().ToUpperInvariant();
        return await context.Users.AsNoTracking()
            .Where(u => u.Id != excludeUserId && u.NormalizedEmail != null && u.NormalizedEmail.StartsWith(normalized))
            .OrderBy(u => u.NormalizedEmail)
            .Take(take)
            .ProjectToType<UserDto>(mapperConfig)
            .ToListAsync(ct);
    }

    public Task<int> CountExistingAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        context.Users.CountAsync(u => ids.Contains(u.Id), ct);
}
