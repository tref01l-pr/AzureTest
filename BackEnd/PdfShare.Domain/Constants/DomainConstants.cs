namespace PdfShare.Domain.Constants;

public static class ClaimNames
{
    public const string SessionId = "sessionId";
}

public static class StorageNames
{
    public const string PdfContainer = "pdfs";
    public const string ThumbnailContainer = "thumbnails";
    public const string PdfUploadedQueue = "pdf-uploaded";
}

public static class FileConstraints
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;
    public const int MaxFileNameLength = 255;
    public const string PdfContentType = "application/pdf";
    public const string PdfExtension = ".pdf";

    public static bool IsAcceptedContentType(string? contentType) =>
        string.IsNullOrEmpty(contentType)
        || contentType.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase)
        || contentType.Equals("application/x-pdf", StringComparison.OrdinalIgnoreCase)
        || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

    public static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;
}

public static class ShareLinkConstraints
{
    public const int TokenByteLength = 32;
    public const int MaxUsersPerLink = 100;
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(5);
}
