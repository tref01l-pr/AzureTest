using System.Security.Claims;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PdfShare.Core.Helpers;
using PdfShare.Core.Services.Authentication;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Authentication.Requests;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;
using PdfShare.Domain.Options;

namespace PdfShare.UnitTests.Core;

public class AuthenticationServiceTests
{
    private const string Secret = "unit-test-secret-unit-test-secret-0123456789";
    private static readonly UserCredentialsDto Alice = new(Guid.NewGuid(), "alice@test.local", "User");

    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ISessionsRepository> _sessions = new();
    private readonly FixedTimeProvider _time = new(TestData.Now);
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests() =>
        _sut = new AuthenticationService(
            _identity.Object,
            _sessions.Object,
            Mock.Of<IUsersRepository>(),
            Options.Create(new JWTSecretOptions { Secret = Secret }),
            _time);

    [Fact]
    public async Task Login_InvalidPassword_ReturnsIdentityError()
    {
        _identity.Setup(i => i.CheckPasswordAsync("alice@test.local", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.InvalidCredentials().ToResult<UserCredentialsDto>());

        var result = await _sut.LoginAsync(new LoginRequest { Email = " alice@test.local ", Password = "bad" });

        result.ErrorCode().Should().Be("Auth.InvalidCredentials");
        _sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_Success_StoresOnlyTheHashOfTheRefreshToken()
    {
        _identity.Setup(i => i.CheckPasswordAsync(Alice.Email, "pw", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(Alice));
        Session? stored = null;
        _sessions.Setup(s => s.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => stored = s);

        var result = await _sut.LoginAsync(new LoginRequest { Email = Alice.Email, Password = "pw" });

        result.IsSuccess.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.RefreshTokenHash.Should().Be(SecureTokenHelper.Hash(result.Value.RefreshToken)).And.NotBe(result.Value.RefreshToken);
        result.Value.AccessTokenExpiresAt.Should().Be(TestData.Now.AddMinutes(15));
        result.Value.RefreshTokenExpiresAt.Should().Be(TestData.Now.AddDays(30));
    }

    [Fact]
    public async Task AccessToken_IsAcceptedByAspNetJwtValidation_WithExpectedClaims()
    {
        var sessionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var token = JwtHelper.GenerateAccessToken(Alice, sessionId, Secret, now, now.AddMinutes(15));

        var validation = await new JsonWebTokenHandler { MapInboundClaims = false }.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        });

        validation.IsValid.Should().BeTrue(validation.Exception?.Message);
        var identity = validation.ClaimsIdentity;
        identity.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(Alice.Id.ToString());
        identity.FindFirst(ClaimTypes.Role)!.Value.Should().Be("User");
        identity.FindFirst(ClaimNames.SessionId)!.Value.Should().Be(sessionId.ToString());
    }

    [Fact]
    public async Task Refresh_UnknownToken_IsUnauthorized()
    {
        var result = await _sut.RefreshAccessTokenAsync("not-a-real-token");

        result.ErrorCode().Should().Be("Auth.Unauthorized");
    }

    [Fact]
    public async Task Refresh_ExpiredSession_IsDeletedAndUnauthorized()
    {
        const string token = "expired-token";
        _sessions.Setup(s => s.GetByRefreshTokenHashAsync<SessionDto>(SecureTokenHelper.Hash(token), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDto { Id = Guid.NewGuid(), UserId = Alice.Id, ExpirationDate = TestData.Now });

        var result = await _sut.RefreshAccessTokenAsync(token);

        result.ErrorCode().Should().Be("Auth.Unauthorized");
        _sessions.Verify(s => s.DeleteByRefreshTokenHashAsync(SecureTokenHelper.Hash(token), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Refresh_ValidSession_RotatesRefreshToken()
    {
        const string oldToken = "old-token";
        var session = new SessionDto { Id = Guid.NewGuid(), UserId = Alice.Id, ExpirationDate = TestData.Now.AddDays(1) };
        _sessions.Setup(s => s.GetByRefreshTokenHashAsync<SessionDto>(SecureTokenHelper.Hash(oldToken), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessions.Setup(s => s.RotateRefreshTokenAsync(session.Id, It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _identity.Setup(i => i.GetCredentialsAsync(Alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(Alice));

        var result = await _sut.RefreshAccessTokenAsync(oldToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBe(oldToken);
        _sessions.Verify(s => s.RotateRefreshTokenAsync(session.Id, SecureTokenHelper.Hash(result.Value.RefreshToken), TestData.Now.AddDays(30), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ChangePassword_SignsOutOtherSessionsOnly()
    {
        var current = Guid.NewGuid();
        _identity.Setup(i => i.ChangePasswordAsync(Alice.Id, "old", "newPassword1", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var result = await _sut.ChangePasswordAsync(Alice.Id, current, new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "newPassword1", ConfirmNewPassword = "newPassword1" });

        result.IsSuccess.Should().BeTrue();
        _sessions.Verify(s => s.DeleteByUserIdAsync(Alice.Id, current, It.IsAny<CancellationToken>()));
    }
}
