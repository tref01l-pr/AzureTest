namespace PdfShare.SharedKernel;

public static class ResultExtensions
{
    public const string GeneralFailureCode = "General.Failure";

    public static Error GetErrorObject(this string error)
    {
        if (string.IsNullOrEmpty(error))
        {
            return new Error(GeneralFailureCode, "Unknown error.");
        }

        var parts = error.Split(TextDelimiters.Separator, 2);
        return parts.Length == 2
            ? new Error(parts[0], parts[1])
            : new Error(GeneralFailureCode, error);
    }
}
