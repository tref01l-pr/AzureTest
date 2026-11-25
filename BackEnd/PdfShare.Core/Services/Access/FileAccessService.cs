using CSharpFunctionalExtensions;
using PdfShare.Domain;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.Core.Services.Access;

public sealed class FileAccessService(TimeProvider timeProvider) : IFileAccessService
{
    public Result EnsureCanDownload(ShareLinkAccessDto link, Guid? userId, bool isListedUser)
    {
        if (link.IsRevoked)
        {
            return ShareLinkErrors.NotFound();
        }

        if (link.ExpiresAt is not null && link.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return ShareLinkErrors.Expired();
        }

        if (link.FileStatus != FileStatus.Ready)
        {
            return FileErrors.NotReady();
        }

        if (link.Type == ShareLinkType.Public)
        {
            return Result.Success();
        }

        if (userId is null)
        {
            return ShareLinkErrors.LoginRequired();
        }

        return userId == link.FileOwnerId || isListedUser
            ? Result.Success()
            : ShareLinkErrors.Forbidden();
    }
}
