using System.Net.Mime;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PdfShare.API.DIExtensions;
using PdfShare.API.Extensions;
using PdfShare.Domain.DTO.Authentication.Requests;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.API.Controllers;

/// <summary>Registration, sign-in, token refresh and account management.</summary>
[Authorize]
[Route("")]
public sealed class UsersAccountController(IAuthenticationService authenticationService, TypeAdapterConfig mapperConfig) : BaseController
{
    /// <summary>Creates a user account and signs it in.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [HttpPost("user-registration")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Register([FromBody] UserRegistrationRequest request, CancellationToken ct)
    {
        var result = await authenticationService.RegisterAsync(request, ct);
        return result.IsFailure ? result.ToProblemDetails() : SignIn(result.Value);
    }

    /// <summary>Signs in with email and password.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [HttpPost("login")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authenticationService.LoginAsync(request, ct);
        return result.IsFailure ? result.ToProblemDetails() : SignIn(result.Value);
    }

    /// <summary>Issues a new access token using the refresh-token cookie (and rotates the cookie).</summary>
    // Not rate-limited: runs on every page load; the 256-bit token can't be brute-forced.
    [AllowAnonymous]
    [HttpPost("refresh-access-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthenticationResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> RefreshAccessToken(CancellationToken ct)
    {
        var result = await authenticationService.RefreshAccessTokenAsync(Request.Cookies[AuthenticationCookieOptions.RefreshTokenCookieName], ct);
        if (result.IsFailure)
        {
            Response.Cookies.Append(AuthenticationCookieOptions.RefreshTokenCookieName, string.Empty, AuthenticationCookieOptions.CreateExpired(Request));
            return result.ToProblemDetails();
        }

        return SignIn(result.Value);
    }

    /// <summary>Ends the current session (works even with an expired access token).</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> Logout(CancellationToken ct)
    {
        await authenticationService.LogoutAsync(Request.Cookies[AuthenticationCookieOptions.RefreshTokenCookieName], ct);
        Response.Cookies.Append(AuthenticationCookieOptions.RefreshTokenCookieName, string.Empty, AuthenticationCookieOptions.CreateExpired(Request));
        return Results.NoContent();
    }

    /// <summary>Returns the signed-in user's account.</summary>
    [HttpGet("account")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserAccountDto))]
    public async Task<IResult> GetAccount(CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await authenticationService.GetAccountAsync(UserId.Value, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    /// <summary>Changes the password and signs out all other sessions.</summary>
    [HttpPost("change-password")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await authenticationService.ChangePasswordAsync(UserId.Value, SessionId, request, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private IResult SignIn(AuthenticationResult result)
    {
        Response.Cookies.Append(
            AuthenticationCookieOptions.RefreshTokenCookieName,
            result.RefreshToken,
            AuthenticationCookieOptions.Create(Request, result.RefreshTokenExpiresAt));

        return Results.Ok(result.Adapt<AuthenticationResponse>(mapperConfig));
    }
}
