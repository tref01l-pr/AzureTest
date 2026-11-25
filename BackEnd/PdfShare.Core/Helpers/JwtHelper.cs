using System.Security.Claims;
using JWT.Algorithms;
using JWT.Builder;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Authentication.Responses;

namespace PdfShare.Core.Helpers;

public static class JwtHelper
{
    public static string GenerateAccessToken(UserCredentialsDto user, Guid sessionId, string secret, DateTimeOffset now, DateTimeOffset expiresAt) =>
        JwtBuilder.Create()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(secret)
            .AddClaim(ClaimName.IssuedAt, now.ToUnixTimeSeconds())
            .AddClaim(ClaimName.ExpirationTime, expiresAt.ToUnixTimeSeconds())
            .AddClaim(ClaimName.JwtId, Guid.NewGuid().ToString("N"))
            .AddClaim(ClaimTypes.NameIdentifier, user.Id.ToString())
            .AddClaim(ClaimTypes.Name, user.Email)
            .AddClaim(ClaimTypes.Role, user.Role)
            .AddClaim(ClaimNames.SessionId, sessionId.ToString())
            .Encode();
}
