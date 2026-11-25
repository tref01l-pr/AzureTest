using CSharpFunctionalExtensions;
using PdfShare.Domain.DTO.Authentication.Requests;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.DTO.Common;
using PdfShare.Domain.DTO.Files.Requests;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.ShareLinks.Requests;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.DTO.Users;

namespace PdfShare.Domain.Interfaces.CoreInterfaces;

public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> RegisterAsync(UserRegistrationRequest request, CancellationToken ct = default);
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<Result<AuthenticationResult>> RefreshAccessTokenAsync(string? refreshToken, CancellationToken ct = default);

    Task<Result> LogoutAsync(string? refreshToken, CancellationToken ct = default);
    Task<Result<UserAccountDto>> GetAccountAsync(Guid userId, CancellationToken ct = default);

    Task<Result> ChangePasswordAsync(Guid userId, Guid? currentSessionId, ChangePasswordRequest request, CancellationToken ct = default);
}

public interface IFilesService
{
    Task<Result<PdfFileDto>> UploadAsync(Guid ownerId, UploadPdfCommand command, CancellationToken ct = default);
    Task<PagedResult<PdfFileDto>> GetMyFilesAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<SharedFileDto>> GetSharedWithMeAsync(Guid userId, CancellationToken ct = default);
    Task<Result<DownloadUrlDto>> GetDownloadUrlAsync(Guid ownerId, Guid fileId, CancellationToken ct = default);
    Task<Result<DownloadUrlDto>> GetThumbnailUrlAsync(Guid ownerId, Guid fileId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid ownerId, Guid fileId, CancellationToken ct = default);
}

public interface IShareLinksService
{
    Task<Result<ShareLinkDto>> CreateAsync(Guid ownerId, Guid fileId, CreateShareLinkRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShareLinkDto>>> GetByFileAsync(Guid ownerId, Guid fileId, CancellationToken ct = default);
    Task<Result<ShareLinkDto>> UpdateUsersAsync(Guid ownerId, Guid linkId, UpdateShareLinkUsersRequest request, CancellationToken ct = default);
    Task<Result> RevokeAsync(Guid ownerId, Guid linkId, CancellationToken ct = default);

    Task<Result<SharedFileInfoDto>> GetSharedFileInfoAsync(string token, Guid? userId, CancellationToken ct = default);

    Task<Result<DownloadUrlDto>> GetSharedDownloadUrlAsync(string token, Guid? userId, CancellationToken ct = default);
}

public interface IFileAccessService
{
    Result EnsureCanDownload(ShareLinkAccessDto link, Guid? userId, bool isListedUser);
}

public interface IUsersService
{
    Task<IReadOnlyList<UserDto>> SearchAsync(string? emailFragment, Guid currentUserId, CancellationToken ct = default);
}
