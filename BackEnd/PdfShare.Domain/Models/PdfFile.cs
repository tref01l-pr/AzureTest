using CSharpFunctionalExtensions;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Errors;

namespace PdfShare.Domain.Models;

public sealed record PdfFile
{
    private PdfFile()
    {
    }

    public Guid Id { get; private init; }
    public Guid OwnerId { get; private init; }
    public string OriginalFileName { get; private init; } = string.Empty;
    public string BlobName { get; private init; } = string.Empty;
    public long SizeBytes { get; private init; }
    public FileStatus Status { get; private init; }
    public DateTimeOffset UploadedAt { get; private init; }

    public static Result<PdfFile> Create(Guid ownerId, string? fileName, string? contentType, long sizeBytes, DateTimeOffset now)
    {
        if (sizeBytes <= 0)
        {
            return FileErrors.Empty().ToResult<PdfFile>();
        }

        if (sizeBytes > FileConstraints.MaxFileSizeBytes)
        {
            return FileErrors.TooLarge(FileConstraints.MaxFileSizeBytes).ToResult<PdfFile>();
        }

        var safeName = SanitizeFileName(fileName);
        if (safeName is null)
        {
            return FileErrors.InvalidName().ToResult<PdfFile>();
        }

        if (!safeName.EndsWith(FileConstraints.PdfExtension, StringComparison.OrdinalIgnoreCase)
            || !FileConstraints.IsAcceptedContentType(contentType))
        {
            return FileErrors.InvalidType().ToResult<PdfFile>();
        }

        var id = Guid.NewGuid();
        return new PdfFile
        {
            Id = id,
            OwnerId = ownerId,
            OriginalFileName = safeName,
            BlobName = BuildBlobName(ownerId, id),
            SizeBytes = sizeBytes,
            Status = FileStatus.Processing,
            UploadedAt = now,
        };
    }

    public static string BuildBlobName(Guid ownerId, Guid fileId) => $"{ownerId:N}/{fileId:N}.pdf";

    public static string BuildThumbnailBlobName(Guid ownerId, Guid fileId) => $"{ownerId:N}/{fileId:N}.png";

    private static string? SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
        name = new string(name.Where(c => !char.IsControl(c) && c != '"').ToArray());

        if (name.Length == 0 || name == FileConstraints.PdfExtension)
        {
            return null;
        }

        if (name.Length > FileConstraints.MaxFileNameLength)
        {
            var extension = Path.GetExtension(name);
            name = name[..(FileConstraints.MaxFileNameLength - extension.Length)] + extension;
        }

        return name;
    }
}
