using CSharpFunctionalExtensions;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.DTO.Common;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.Users;
using PdfShare.Domain.Models;

namespace PdfShare.Domain.Interfaces.DataAccessInterfaces;

public interface IPdfFilesRepository
{
    Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class;
    Task<TProjectTo?> GetOwnedAsync<TProjectTo>(Guid id, Guid ownerId, CancellationToken ct = default) where TProjectTo : class;
    Task<PagedResult<TProjectTo>> GetByOwnerAsync<TProjectTo>(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<SharedFileDto>> GetSharedWithUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);
    Task CreateAsync(PdfFile file, CancellationToken ct = default);

    Task<bool> MarkReadyAsync(Guid id, int pageCount, string? thumbnailBlobName, CancellationToken ct = default);

    Task<bool> MarkRejectedAsync(Guid id, string reason, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default);
}

public interface IShareLinksRepository
{
    Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class;
    Task<TProjectTo?> GetByTokenAsync<TProjectTo>(string token, CancellationToken ct = default) where TProjectTo : class;
    Task<IReadOnlyList<TProjectTo>> GetByFileAsync<TProjectTo>(Guid fileId, CancellationToken ct = default);
    Task CreateAsync(ShareLink link, CancellationToken ct = default);
    Task ReplaceUsersAsync(Guid linkId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid linkId, CancellationToken ct = default);
    Task<bool> HasUserAsync(Guid linkId, Guid userId, CancellationToken ct = default);
}

public interface ISessionsRepository
{
    Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class;
    Task<TProjectTo?> GetByRefreshTokenHashAsync<TProjectTo>(string refreshTokenHash, CancellationToken ct = default) where TProjectTo : class;
    Task<bool> ExistsAsync(Guid id, DateTimeOffset now, CancellationToken ct = default);
    Task CreateAsync(Session session, CancellationToken ct = default);
    Task<bool> RotateRefreshTokenAsync(Guid id, string newRefreshTokenHash, DateTimeOffset newExpirationDate, CancellationToken ct = default);
    Task DeleteByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default);
    Task DeleteByUserIdAsync(Guid userId, Guid? exceptSessionId = null, CancellationToken ct = default);
}

public interface IUsersRepository
{
    Task<TProjectTo?> GetByIdAsync<TProjectTo>(Guid id, CancellationToken ct = default) where TProjectTo : class;
    Task<IReadOnlyList<UserDto>> SearchByEmailAsync(string emailFragment, Guid excludeUserId, int take, CancellationToken ct = default);
    Task<int> CountExistingAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}

public interface IIdentityService
{
    Task<Result<UserCredentialsDto>> RegisterAsync(string email, string password, Roles role, CancellationToken ct = default);
    Task<Result<UserCredentialsDto>> CheckPasswordAsync(string email, string password, CancellationToken ct = default);
    Task<Result<UserCredentialsDto>> GetCredentialsAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> action, CancellationToken ct = default);
}
