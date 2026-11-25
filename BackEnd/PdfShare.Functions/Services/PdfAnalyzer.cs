using System.Runtime.Versioning;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using PDFtoImage;

namespace PdfShare.Functions.Services;

public sealed record PdfAnalysis(int PageCount, byte[] ThumbnailPng);

public interface IPdfAnalyzer
{
    Result<PdfAnalysis> Analyze(byte[] pdf);
}

public sealed class PdfAnalyzer(ILogger<PdfAnalyzer> logger) : IPdfAnalyzer
{
    public const int ThumbnailWidth = 400;
    public const string InvalidPdfReason = "The file is not a valid PDF or is password-protected.";

    public Result<PdfAnalysis> Analyze(byte[] pdf)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return AnalyzeCore(pdf);
        }

        throw new PlatformNotSupportedException("PDF rendering requires Windows, Linux or macOS.");
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private Result<PdfAnalysis> AnalyzeCore(byte[] pdf)
    {
        try
        {
            var pageCount = Conversion.GetPageCount(pdf);
            if (pageCount < 1)
            {
                return Result.Failure<PdfAnalysis>("The PDF has no pages.");
            }

            using var png = new MemoryStream();
            Conversion.SavePng(png, pdf, page: Index.Start, options: new RenderOptions(Width: ThumbnailWidth, WithAspectRatio: true));
            return new PdfAnalysis(pageCount, png.ToArray());
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            logger.LogWarning(ex, "PDF analysis failed");
            return Result.Failure<PdfAnalysis>(InvalidPdfReason);
        }
    }
}
