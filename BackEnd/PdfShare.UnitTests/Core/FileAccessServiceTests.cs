using PdfShare.Core.Services.Access;
using PdfShare.Domain;
using PdfShare.Domain.DTO.ShareLinks.Responses;

namespace PdfShare.UnitTests.Core;

public class FileAccessServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();
    private readonly FileAccessService _sut = new(new FixedTimeProvider(TestData.Now));

    private static ShareLinkAccessDto Link(
        ShareLinkType type = ShareLinkType.Restricted,
        bool revoked = false,
        DateTimeOffset? expiresAt = null,
        FileStatus status = FileStatus.Ready) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        IsRevoked = revoked,
        ExpiresAt = expiresAt,
        FileStatus = status,
        FileOwnerId = Owner,
    };

    [Fact]
    public void PublicLink_AnonymousVisitor_CanDownload() =>
        _sut.EnsureCanDownload(Link(ShareLinkType.Public), userId: null, isListedUser: false).IsSuccess.Should().BeTrue();

    [Fact]
    public void RestrictedLink_Anonymous_MustSignIn() =>
        _sut.EnsureCanDownload(Link(), userId: null, isListedUser: false).ErrorCode().Should().Be("ShareLink.Unauthorized");

    [Fact]
    public void RestrictedLink_ListedUser_CanDownload() =>
        _sut.EnsureCanDownload(Link(), Stranger, isListedUser: true).IsSuccess.Should().BeTrue();

    [Fact]
    public void RestrictedLink_Owner_CanDownload() =>
        _sut.EnsureCanDownload(Link(), Owner, isListedUser: false).IsSuccess.Should().BeTrue();

    [Fact]
    public void RestrictedLink_UserNotOnTheList_IsForbidden() =>
        _sut.EnsureCanDownload(Link(), Stranger, isListedUser: false).ErrorCode().Should().Be("ShareLink.Forbidden");

    [Theory]
    [InlineData(ShareLinkType.Public)]
    [InlineData(ShareLinkType.Restricted)]
    public void RevokedLink_LooksLikeMissing_EvenForListedUsers(ShareLinkType type) =>
        _sut.EnsureCanDownload(Link(type, revoked: true), Stranger, isListedUser: true).ErrorCode().Should().Be("ShareLink.NotFound");

    [Fact]
    public void ExpiredLink_IsGone() =>
        _sut.EnsureCanDownload(Link(ShareLinkType.Public, expiresAt: TestData.Now), null, false).ErrorCode().Should().Be("ShareLink.Expired");

    [Fact]
    public void LinkExpiringLater_StillWorks() =>
        _sut.EnsureCanDownload(Link(ShareLinkType.Public, expiresAt: TestData.Now.AddSeconds(1)), null, false).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData(FileStatus.Processing)]
    [InlineData(FileStatus.Rejected)]
    public void FileNotReady_CannotBeDownloadedThroughLink(FileStatus status) =>
        _sut.EnsureCanDownload(Link(ShareLinkType.Public, status: status), null, false).ErrorCode().Should().Be("File.Conflict");
}
