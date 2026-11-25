using System.ComponentModel.DataAnnotations;
using PdfShare.Domain.DTO.Users;

namespace PdfShare.Domain.DTO.ShareLinks.Requests
{
    public sealed class CreateShareLinkRequest
    {
        [Required]
        public ShareLinkType Type { get; init; }

        public List<Guid> UserIds { get; init; } = [];

        public DateTimeOffset? ExpiresAt { get; init; }
    }

    public sealed class UpdateShareLinkUsersRequest
    {
        [Required]
        public List<Guid> UserIds { get; init; } = [];
    }
}

namespace PdfShare.Domain.DTO.ShareLinks.Responses
{
    public sealed class ShareLinkDto
    {
        public Guid Id { get; init; }
        public Guid FileId { get; init; }
        public string Token { get; init; } = string.Empty;
        public ShareLinkType Type { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public bool IsRevoked { get; init; }
        public List<UserDto> Users { get; init; } = [];
    }

    public sealed class SharedFileInfoDto
    {
        public string FileName { get; init; } = string.Empty;
        public long SizeBytes { get; init; }
        public int? PageCount { get; init; }
        public string OwnerEmail { get; init; } = string.Empty;
        public ShareLinkType Type { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }

    public sealed class ShareLinkAccessDto
    {
        public Guid Id { get; init; }
        public Guid FileId { get; init; }
        public ShareLinkType Type { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public bool IsRevoked { get; init; }
        public Guid FileOwnerId { get; init; }
        public FileStatus FileStatus { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FileBlobName { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public int? FilePageCount { get; init; }
        public string OwnerEmail { get; init; } = string.Empty;
    }
}
