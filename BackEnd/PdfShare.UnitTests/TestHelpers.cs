using System.Text;
using CSharpFunctionalExtensions;
using PdfShare.SharedKernel;

namespace PdfShare.UnitTests;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    public static byte[] MinimalPdf() => Encoding.ASCII.GetBytes(
        "%PDF-1.4\n" +
        "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n" +
        "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n" +
        "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >> endobj\n" +
        "trailer << /Root 1 0 R >>\n" +
        "%%EOF\n");

    public static string ErrorCode(this Result result) => result.Error.GetErrorObject().Code;

    public static string ErrorCode<T>(this Result<T> result) => result.Error.GetErrorObject().Code;
}
