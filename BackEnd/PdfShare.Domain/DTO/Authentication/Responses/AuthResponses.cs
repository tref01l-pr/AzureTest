namespace PdfShare.Domain.DTO.Authentication.Responses;

public class AuthenticationResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
}

public sealed class AuthenticationResult : AuthenticationResponse
{
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

public sealed class UserAccountDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record UserCredentialsDto(Guid Id, string Email, string Role);

public sealed class SessionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset ExpirationDate { get; init; }
}
