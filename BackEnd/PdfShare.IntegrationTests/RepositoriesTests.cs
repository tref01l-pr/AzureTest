using PdfShare.Domain;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Models;

namespace PdfShare.IntegrationTests;

public class PdfFilesRepositoryTests : IntegrationTestBase
{
    [Test]
    public async Task MarkReady_IsIdempotent_SecondCallIsNoOp()
    {
        var owner = await CreateUserAsync();
        var file = await CreateFileAsync(owner.Id, ready: false);
        var repository = Get<IPdfFilesRepository>();

        (await repository.MarkReadyAsync(file.Id, 5, "thumb.png")).Should().BeTrue();
        (await repository.MarkReadyAsync(file.Id, 9, "other.png")).Should().BeFalse();
        (await repository.MarkRejectedAsync(file.Id, "late")).Should().BeFalse();

        var stored = await repository.GetByIdAsync<PdfFileDto>(file.Id);
        stored!.Status.Should().Be(FileStatus.Ready);
        stored.PageCount.Should().Be(5);
        stored.HasThumbnail.Should().BeTrue();
    }

    [Test]
    public async Task GetOwned_OtherUsersFile_ReturnsNull()
    {
        var owner = await CreateUserAsync();
        var stranger = await CreateUserAsync();
        var file = await CreateFileAsync(owner.Id);

        (await Get<IPdfFilesRepository>().GetOwnedAsync<PdfFileDto>(file.Id, stranger.Id)).Should().BeNull();
    }

    [Test]
    public async Task GetByOwner_PagesNewestFirst()
    {
        var owner = await CreateUserAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateFileAsync(owner.Id);
        }

        var page = await Get<IPdfFilesRepository>().GetByOwnerAsync<PdfFileDto>(owner.Id, page: 1, pageSize: 2);

        page.Total.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(2).And.BeInDescendingOrder(f => f.UploadedAt);
    }

    [Test]
    public async Task Delete_CascadesToLinksAndLinkUsers()
    {
        var owner = await CreateUserAsync();
        var bob = await CreateUserAsync();
        var file = await CreateFileAsync(owner.Id);
        var link = ShareLink.Create(file.Id, owner.Id, Guid.NewGuid().ToString("N"), ShareLinkType.Restricted, [bob.Id], null, DateTimeOffset.UtcNow).Value;
        await Get<IShareLinksRepository>().CreateAsync(link);

        (await Get<IPdfFilesRepository>().DeleteAsync(file.Id, owner.Id)).Should().BeTrue();

        (await Get<IShareLinksRepository>().GetByIdAsync<ShareLinkDto>(link.Id)).Should().BeNull();
        (await Get<IShareLinksRepository>().HasUserAsync(link.Id, bob.Id)).Should().BeFalse();
    }
}

public class SharedWithMeTests : IntegrationTestBase
{
    [Test]
    public async Task OnlyActiveRestrictedLinksToReadyFiles_AreListed()
    {
        var owner = await CreateUserAsync("owner-" + Guid.NewGuid().ToString("N")[..6] + "@test.local");
        var bob = await CreateUserAsync();
        var links = Get<IShareLinksRepository>();
        var now = DateTimeOffset.UtcNow;

        var visible = await CreateFileAsync(owner.Id);
        await links.CreateAsync(ShareLink.Create(visible.Id, owner.Id, "visible-" + Guid.NewGuid().ToString("N"), ShareLinkType.Restricted, [bob.Id], now.AddDays(1), now).Value);

        var revokedFile = await CreateFileAsync(owner.Id);
        var revoked = ShareLink.Create(revokedFile.Id, owner.Id, "revoked-" + Guid.NewGuid().ToString("N"), ShareLinkType.Restricted, [bob.Id], null, now).Value;
        await links.CreateAsync(revoked);
        await links.RevokeAsync(revoked.Id);

        var processing = await CreateFileAsync(owner.Id, ready: false);
        await links.CreateAsync(ShareLink.Create(processing.Id, owner.Id, "proc-" + Guid.NewGuid().ToString("N"), ShareLinkType.Restricted, [bob.Id], null, now).Value);

        var shared = await Get<IPdfFilesRepository>().GetSharedWithUserAsync(bob.Id, now);

        shared.Should().ContainSingle().Which.FileId.Should().Be(visible.Id);
        shared[0].OwnerEmail.Should().Be(owner.Email);
        (await Get<IPdfFilesRepository>().GetSharedWithUserAsync(bob.Id, now.AddDays(2))).Should().BeEmpty("the link has expired by then");
    }
}

public class ShareLinksRepositoryTests : IntegrationTestBase
{
    [Test]
    public async Task Projections_IncludeUsersAndFileAccessData()
    {
        var owner = await CreateUserAsync();
        var bob = await CreateUserAsync();
        var carol = await CreateUserAsync();
        var file = await CreateFileAsync(owner.Id);
        var token = Guid.NewGuid().ToString("N");
        var link = ShareLink.Create(file.Id, owner.Id, token, ShareLinkType.Restricted, [bob.Id, carol.Id], null, DateTimeOffset.UtcNow).Value;
        var repository = Get<IShareLinksRepository>();
        await repository.CreateAsync(link);

        var dto = await repository.GetByIdAsync<ShareLinkDto>(link.Id);
        dto!.Users.Select(u => u.Id).Should().BeEquivalentTo([bob.Id, carol.Id]);

        var access = await repository.GetByTokenAsync<ShareLinkAccessDto>(token);
        access!.FileOwnerId.Should().Be(owner.Id);
        access.FileStatus.Should().Be(FileStatus.Ready);
        access.OwnerEmail.Should().Be(owner.Email);
        access.FileBlobName.Should().Be(file.BlobName);
    }

    [Test]
    public async Task ReplaceUsers_SwapsTheList()
    {
        var owner = await CreateUserAsync();
        var bob = await CreateUserAsync();
        var carol = await CreateUserAsync();
        var file = await CreateFileAsync(owner.Id);
        var link = ShareLink.Create(file.Id, owner.Id, Guid.NewGuid().ToString("N"), ShareLinkType.Restricted, [bob.Id], null, DateTimeOffset.UtcNow).Value;
        var repository = Get<IShareLinksRepository>();
        await repository.CreateAsync(link);

        await repository.ReplaceUsersAsync(link.Id, [carol.Id]);

        (await repository.HasUserAsync(link.Id, bob.Id)).Should().BeFalse();
        (await repository.HasUserAsync(link.Id, carol.Id)).Should().BeTrue();
    }
}

public class SessionsAndUsersTests : IntegrationTestBase
{
    [Test]
    public async Task Session_RotateAndDeleteOthers()
    {
        var user = await CreateUserAsync();
        var sessions = Get<ISessionsRepository>();
        var now = DateTimeOffset.UtcNow;
        var current = Session.Create(user.Id, new string('a', 64), now, TimeSpan.FromDays(1));
        var other = Session.Create(user.Id, new string('b', 64), now, TimeSpan.FromDays(1));
        await sessions.CreateAsync(current);
        await sessions.CreateAsync(other);

        (await sessions.RotateRefreshTokenAsync(current.Id, new string('c', 64), now.AddDays(2))).Should().BeTrue();
        (await sessions.GetByRefreshTokenHashAsync<PdfShare.Domain.DTO.Authentication.Responses.SessionDto>(new string('a', 64))).Should().BeNull();

        await sessions.DeleteByUserIdAsync(user.Id, exceptSessionId: current.Id);

        (await sessions.ExistsAsync(current.Id, now)).Should().BeTrue();
        (await sessions.ExistsAsync(other.Id, now)).Should().BeFalse();
    }

    [Test]
    public async Task SearchByEmail_PrefixMatch_ExcludesCaller()
    {
        var prefix = "zz" + Guid.NewGuid().ToString("N")[..6];
        var me = await CreateUserAsync($"{prefix}-me@test.local");
        var other = await CreateUserAsync($"{prefix}-other@test.local");

        var found = await Get<IUsersRepository>().SearchByEmailAsync(prefix.ToUpperInvariant(), me.Id, 10);

        found.Should().ContainSingle().Which.Id.Should().Be(other.Id);
    }
}
