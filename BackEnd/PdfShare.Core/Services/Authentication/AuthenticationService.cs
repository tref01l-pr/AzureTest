using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using PdfShare.Core.Helpers;
using PdfShare.Domain;
using PdfShare.Domain.DTO.Authentication.Requests;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.CoreInterfaces;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;
using PdfShare.Domain.Options;

namespace PdfShare.Core.Services.Authentication;

public sealed class AuthenticationService(
    IIdentityService identityService,
    ISessionsRepository sessionsRepository,
    IUsersRepository usersRepository,
    IOptions<JWTSecretOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthenticationService
{
    private readonly JWTSecretOptions _jwt = jwtOptions.Value;

    public async Task<Result<AuthenticationResult>> RegisterAsync(UserRegistrationRequest request, CancellationToken ct = default)
    {
        var registered = await identityService.RegisterAsync(NormalizeEmail(request.Email), request.Password, Roles.User, ct);
        return registered.IsFailure
            ? Result.Failure<AuthenticationResult>(registered.Error)
            : await CreateSessionAsync(registered.Value, ct);
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var credentials = await identityService.CheckPasswordAsync(NormalizeEmail(request.Email), request.Password, ct);
        return credentials.IsFailure
            ? Result.Failure<AuthenticationResult>(credentials.Error)
            : await CreateSessionAsync(credentials.Value, ct);
    }

    public async Task<Result<AuthenticationResult>> RefreshAccessTokenAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthErrors.InvalidRefreshToken().ToResult<AuthenticationResult>();
        }

        var now = timeProvider.GetUtcNow();
        var hash = SecureTokenHelper.Hash(refreshToken);
        var session = await sessionsRepository.GetByRefreshTokenHashAsync<SessionDto>(hash, ct);
        if (session is null || session.ExpirationDate <= now)
        {
            if (session is not null)
            {
                await sessionsRepository.DeleteByRefreshTokenHashAsync(hash, ct);
            }

            return AuthErrors.InvalidRefreshToken().ToResult<AuthenticationResult>();
        }

        var credentials = await identityService.GetCredentialsAsync(session.UserId, ct);
        if (credentials.IsFailure)
        {
            return AuthErrors.InvalidRefreshToken().ToResult<AuthenticationResult>();
        }

        var newRefreshToken = SecureTokenHelper.Generate();
        var refreshExpiresAt = now.Add(_jwt.RefreshTokenLifetime);
        var rotated = await sessionsRepository.RotateRefreshTokenAsync(session.Id, SecureTokenHelper.Hash(newRefreshToken), refreshExpiresAt, ct);
        if (!rotated)
        {
            return AuthErrors.InvalidRefreshToken().ToResult<AuthenticationResult>();
        }

        return BuildResult(credentials.Value, session.Id, newRefreshToken, refreshExpiresAt, now);
    }

    public async Task<Result> LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await sessionsRepository.DeleteByRefreshTokenHashAsync(SecureTokenHelper.Hash(refreshToken), ct);
        }

        return Result.Success();
    }

    public async Task<Result<UserAccountDto>> GetAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await usersRepository.GetByIdAsync<UserAccountDto>(userId, ct);
        return account is null ? AuthErrors.UserNotFound().ToResult<UserAccountDto>() : account;
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, Guid? currentSessionId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var changed = await identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
        if (changed.IsFailure)
        {
            return changed;
        }

        await sessionsRepository.DeleteByUserIdAsync(userId, currentSessionId, ct);
        return Result.Success();
    }

    private async Task<Result<AuthenticationResult>> CreateSessionAsync(UserCredentialsDto user, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var refreshToken = SecureTokenHelper.Generate();
        var session = Session.Create(user.Id, SecureTokenHelper.Hash(refreshToken), now, _jwt.RefreshTokenLifetime);
        await sessionsRepository.CreateAsync(session, ct);
        return BuildResult(user, session.Id, refreshToken, session.ExpirationDate, now);
    }

    private AuthenticationResult BuildResult(UserCredentialsDto user, Guid sessionId, string refreshToken, DateTimeOffset refreshExpiresAt, DateTimeOffset now)
    {
        var accessExpiresAt = now.Add(_jwt.AccessTokenLifetime);
        return new AuthenticationResult
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = JwtHelper.GenerateAccessToken(user, sessionId, _jwt.Secret, now, accessExpiresAt),
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
        };
    }

    private static string NormalizeEmail(string email) => email.Trim();
}
