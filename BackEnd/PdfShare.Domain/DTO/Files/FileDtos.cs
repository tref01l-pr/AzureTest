namespace PdfShare.Domain.DTO.Files.Requests
{
    public sealed record UploadPdfCommand(Stream Content, string? FileName, string? ContentType, long Length);
}

namespace PdfShare.Domain.DTO.Files.Responses
{
    public sealed class PdfFileDto
    {
        public Guid Id { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public long SizeBytes { get; init; }
        public FileStatus Status { get; init; }
        public DateTimeOffset UploadedAt { get; init; }
        public int? PageCount { get; init; }
        public string? RejectReason { get; init; }
        public bool HasThumbnail { get; init; }
    }

    public sealed class SharedFileDto
    {
        public Guid FileId { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public long SizeBytes { get; init; }
        public int? PageCount { get; init; }
        public string OwnerEmail { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
        public DateTimeOffset SharedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }

    public sealed class PdfFileStorageDto
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public string BlobName { get; init; } = string.Empty;
        public string? ThumbnailBlobName { get; init; }
        public FileStatus Status { get; init; }
    }

    public sealed record DownloadUrlDto(Uri Url, DateTimeOffset ExpiresAt);
}
