namespace PdfShare.API.Extensions;

public static class AuthenticationCookieOptions
{
    public const string RefreshTokenCookieName = "refreshtoken";

    public static CookieOptions Create(HttpRequest request, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps || !IsLocalhost(request),
        SameSite = SameSiteMode.Strict, // works because the SPA reaches the API through the same-origin /api proxy
        Path = "/",
        Expires = expires,
        IsEssential = true,
    };

    public static CookieOptions CreateExpired(HttpRequest request) => Create(request, DateTimeOffset.UnixEpoch);

    private static bool IsLocalhost(HttpRequest request) =>
        request.Host.Host is "localhost" or "127.0.0.1" or "[::1]" or "backend";
}
