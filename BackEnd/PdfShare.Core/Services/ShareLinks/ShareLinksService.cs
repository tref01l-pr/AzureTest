using CSharpFunctionalExtensions;
using PdfShare.Core.Helpers;
using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.ShareLinks.Requests;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.CoreInterfaces;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Interfaces.InfrastructureInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.Core.Services.ShareLinks;

public sealed class ShareLinksService(
    IShareLinksRepository linksRepository,
    IPdfFilesRepository filesRepository,
    IUsersRepository usersRepository,
    IUnitOfWork unitOfWork,
    IFileAccessService accessService,
    IBlobStorageService blobStorage,
    TimeProvider timeProvider) : IShareLinksService
{
    private const int MaxTokenLength = 64;

    public async Task<Result<ShareLinkDto>> CreateAsync(Guid ownerId, Guid fileId, CreateShareLinkRequest request, CancellationToken ct = default)
    {
        var file = await filesRepository.GetOwnedAsync<PdfFileStorageDto>(fileId, ownerId, ct);
        if (file is null)
        {
            return FileErrors.NotFound().ToResult<ShareLinkDto>();
        }

        if (file.Status == FileStatus.Rejected)
        {
            return ShareLinkErrors.FileRejected().ToResult<ShareLinkDto>();
        }

        var created = ShareLink.Create(
            fileId,
            ownerId,
            SecureTokenHelper.Generate(ShareLinkConstraints.TokenByteLength),
            request.Type,
            request.UserIds,
            request.ExpiresAt,
            timeProvider.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(created.Error);
        }

        var usersExist = await EnsureUsersExistAsync(created.Value.UserIds, ct);
        if (usersExist.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(usersExist.Error);
        }

        await linksRepository.CreateAsync(created.Value, ct);
        return (await linksRepository.GetByIdAsync<ShareLinkDto>(created.Value.Id, ct))!;
    }

    public async Task<Result<IReadOnlyList<ShareLinkDto>>> GetByFileAsync(Guid ownerId, Guid fileId, CancellationToken ct = default)
    {
        var file = await filesRepository.GetOwnedAsync<PdfFileStorageDto>(fileId, ownerId, ct);
        if (file is null)
        {
            return FileErrors.NotFound().ToResult<IReadOnlyList<ShareLinkDto>>();
        }

        var links = await linksRepository.GetByFileAsync<ShareLinkDto>(fileId, ct);
        return Result.Success(links);
    }

    public async Task<Result<ShareLinkDto>> UpdateUsersAsync(Guid ownerId, Guid linkId, UpdateShareLinkUsersRequest request, CancellationToken ct = default)
    {
        var link = await GetOwnedActiveLinkAsync(ownerId, linkId, ct);
        if (link.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(link.Error);
        }

        if (link.Value.Type != ShareLinkType.Restricted)
        {
            return ShareLinkErrors.NotRestricted().ToResult<ShareLinkDto>();
        }

        var users = ShareLink.NormalizeUsers(ShareLinkType.Restricted, ownerId, request.UserIds);
        if (users.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(users.Error);
        }

        var usersExist = await EnsureUsersExistAsync(users.Value, ct);
        if (usersExist.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(usersExist.Error);
        }

        var replaced = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await linksRepository.ReplaceUsersAsync(linkId, users.Value, token);
            return Result.Success(true);
        }, ct);
        if (replaced.IsFailure)
        {
            return Result.Failure<ShareLinkDto>(replaced.Error);
        }

        return (await linksRepository.GetByIdAsync<ShareLinkDto>(linkId, ct))!;
    }

    public async Task<Result> RevokeAsync(Guid ownerId, Guid linkId, CancellationToken ct = default)
    {
        var link = await GetOwnedActiveLinkAsync(ownerId, linkId, ct);
        if (link.IsFailure)
        {
            return link;
        }

        await linksRepository.RevokeAsync(linkId, ct);
        return Result.Success();
    }

    public async Task<Result<SharedFileInfoDto>> GetSharedFileInfoAsync(string token, Guid? userId, CancellationToken ct = default)
    {
        var link = await GetAccessibleLinkAsync(token, userId, ct);
        if (link.IsFailure)
        {
            return Result.Failure<SharedFileInfoDto>(link.Error);
        }

        return new SharedFileInfoDto
        {
            FileName = link.Value.FileName,
            SizeBytes = link.Value.FileSizeBytes,
            PageCount = link.Value.FilePageCount,
            OwnerEmail = link.Value.OwnerEmail,
            Type = link.Value.Type,
            ExpiresAt = link.Value.ExpiresAt,
        };
    }

    public async Task<Result<DownloadUrlDto>> GetSharedDownloadUrlAsync(string token, Guid? userId, CancellationToken ct = default)
    {
        var link = await GetAccessibleLinkAsync(token, userId, ct);
        if (link.IsFailure)
        {
            return Result.Failure<DownloadUrlDto>(link.Error);
        }

        var expiresAt = timeProvider.GetUtcNow().Add(ShareLinkConstraints.DownloadUrlLifetime);
        var url = await blobStorage.GetReadSasUriAsync(StorageNames.PdfContainer, link.Value.FileBlobName, link.Value.FileName, expiresAt, ct);
        return new DownloadUrlDto(url, expiresAt);
    }

    private async Task<Result<ShareLinkAccessDto>> GetAccessibleLinkAsync(string token, Guid? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return ShareLinkErrors.NotFound().ToResult<ShareLinkAccessDto>();
        }

        var link = await linksRepository.GetByTokenAsync<ShareLinkAccessDto>(token, ct);
        if (link is null)
        {
            return ShareLinkErrors.NotFound().ToResult<ShareLinkAccessDto>();
        }

        var isListedUser = userId is not null
                           && link.Type == ShareLinkType.Restricted
                           && await linksRepository.HasUserAsync(link.Id, userId.Value, ct);

        var access = accessService.EnsureCanDownload(link, userId, isListedUser);
        return access.IsFailure ? Result.Failure<ShareLinkAccessDto>(access.Error) : link;
    }

    private async Task<Result<ShareLinkAccessDto>> GetOwnedActiveLinkAsync(Guid ownerId, Guid linkId, CancellationToken ct)
    {
        var link = await linksRepository.GetByIdAsync<ShareLinkAccessDto>(linkId, ct);

        return link is null || link.FileOwnerId != ownerId || link.IsRevoked
            ? ShareLinkErrors.NotFound().ToResult<ShareLinkAccessDto>()
            : link;
    }

    private async Task<Result> EnsureUsersExistAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return Result.Success();
        }

        return await usersRepository.CountExistingAsync(userIds, ct) == userIds.Count
            ? Result.Success()
            : ShareLinkErrors.UnknownUsers();
    }
}
