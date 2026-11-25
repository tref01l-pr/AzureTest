using PdfShare.Domain;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Models;

namespace PdfShare.UnitTests.Domain;

public class ShareLinkTests
{
    private static readonly Guid File = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Public_WithoutUsers_Succeeds()
    {
        var result = ShareLink.Create(File, Owner, "token", ShareLinkType.Public, null, null, TestData.Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void Public_WithUsers_Fails() =>
        ShareLink.Create(File, Owner, "t", ShareLinkType.Public, [Guid.NewGuid()], null, TestData.Now)
            .ErrorCode().Should().Be("ShareLink.UsersNotAllowed");

    [Fact]
    public void Restricted_WithoutUsers_Fails() =>
        ShareLink.Create(File, Owner, "t", ShareLinkType.Restricted, [], null, TestData.Now)
            .ErrorCode().Should().Be("ShareLink.UsersRequired");

    [Fact]
    public void Restricted_OnlyOwnerSelected_FailsBecauseOwnerIsImplicit() =>
        ShareLink.Create(File, Owner, "t", ShareLinkType.Restricted, [Owner], null, TestData.Now)
            .ErrorCode().Should().Be("ShareLink.UsersRequired");

    [Fact]
    public void Restricted_RemovesDuplicatesEmptyIdsAndOwner()
    {
        var user = Guid.NewGuid();

        var result = ShareLink.Create(File, Owner, "t", ShareLinkType.Restricted, [user, user, Guid.Empty, Owner], null, TestData.Now);

        result.Value.UserIds.Should().Equal(user);
    }

    [Fact]
    public void Restricted_TooManyUsers_Fails()
    {
        var users = Enumerable.Range(0, ShareLinkConstraints.MaxUsersPerLink + 1).Select(_ => Guid.NewGuid());

        ShareLink.Create(File, Owner, "t", ShareLinkType.Restricted, users, null, TestData.Now)
            .ErrorCode().Should().Be("ShareLink.TooManyUsers");
    }

    [Fact]
    public void ExpirationInThePast_Fails() =>
        ShareLink.Create(File, Owner, "t", ShareLinkType.Public, null, TestData.Now.AddMinutes(-1), TestData.Now)
            .ErrorCode().Should().Be("ShareLink.InvalidExpiration");
}
