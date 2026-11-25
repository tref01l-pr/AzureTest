using CSharpFunctionalExtensions;
using PdfShare.SharedKernel;

namespace PdfShare.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result) => ToProblem(result.Error.GetErrorObject());

    public static IResult ToProblemDetails<T>(this Result<T> result) => ToProblem(result.Error.GetErrorObject());

    public static IResult ToProblem(params Error[] errors)
    {
        var status = GetStatusCode(errors[0].Code);
        return Results.Problem(
            statusCode: status,
            title: GetTitle(status),
            detail: errors[0].Description,
            extensions: new Dictionary<string, object?> { ["errors"] = errors });
    }

    public static int GetStatusCode(string code) => code switch
    {
        _ when code.EndsWith(".NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
        _ when code.EndsWith(".Unauthorized", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
        _ when code.EndsWith(".Forbidden", StringComparison.Ordinal) => StatusCodes.Status403Forbidden,
        _ when code.EndsWith(".Conflict", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
        _ when code.EndsWith(".Expired", StringComparison.Ordinal) => StatusCodes.Status410Gone,
        _ when code.EndsWith(".LockedOut", StringComparison.Ordinal) => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status400BadRequest,
    };

    private static string GetTitle(int status) => status switch
    {
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status410Gone => "Gone",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        _ => "Bad Request",
    };
}
