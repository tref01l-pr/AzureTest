using Mapster;
using PdfShare.DataAccess.SqlServer.Entities.FileEntities;
using PdfShare.DataAccess.SqlServer.Entities.SessionEntities;
using PdfShare.DataAccess.SqlServer.Entities.ShareLinkEntities;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.DTO.Users;
using PdfShare.Domain.Models;

namespace PdfShare.DataAccess.SqlServer;

public sealed class DataAccessMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PdfFile, PdfFileEntity>();
        config.NewConfig<Session, SessionEntity>();
        config.NewConfig<ShareLink, ShareLinkEntity>()
            .Map(d => d.Users, s => s.UserIds.Select(id => new ShareLinkUserEntity { UserId = id }).ToList());

        config.NewConfig<UserEntity, UserDto>();
        config.NewConfig<UserEntity, UserAccountDto>();
        config.NewConfig<SessionEntity, SessionDto>();

        config.NewConfig<PdfFileEntity, PdfFileDto>()
            .Map(d => d.HasThumbnail, s => s.ThumbnailBlobName != null);
        config.NewConfig<PdfFileEntity, PdfFileStorageDto>();

        config.NewConfig<ShareLinkEntity, ShareLinkDto>()
            .Map(d => d.Users, s => s.Users
                .OrderBy(u => u.User.Email)
                .Select(u => new UserDto { Id = u.UserId, Email = u.User.Email! })
                .ToList());

        config.NewConfig<ShareLinkEntity, ShareLinkAccessDto>()
            .Map(d => d.FileOwnerId, s => s.File.OwnerId)
            .Map(d => d.FileStatus, s => s.File.Status)
            .Map(d => d.FileName, s => s.File.OriginalFileName)
            .Map(d => d.FileBlobName, s => s.File.BlobName)
            .Map(d => d.FileSizeBytes, s => s.File.SizeBytes)
            .Map(d => d.FilePageCount, s => s.File.PageCount)
            .Map(d => d.OwnerEmail, s => s.File.Owner.Email!);
    }
}
