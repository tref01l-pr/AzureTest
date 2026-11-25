using CSharpFunctionalExtensions;

namespace PdfShare.SharedKernel;

public sealed record Error(string Code, string Description)
{
    public Result ToResult() => Result.Failure(ToString());

    public Result<T> ToResult<T>() => Result.Failure<T>(ToString());

    public override string ToString() => $"{Code}{TextDelimiters.Separator}{Description}";

    public static implicit operator Result(Error error) => error.ToResult();
}
