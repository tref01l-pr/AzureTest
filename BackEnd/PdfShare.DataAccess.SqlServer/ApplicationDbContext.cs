using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PdfShare.DataAccess.SqlServer.Entities.FileEntities;
using PdfShare.DataAccess.SqlServer.Entities.SessionEntities;
using PdfShare.DataAccess.SqlServer.Entities.ShareLinkEntities;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;

namespace PdfShare.DataAccess.SqlServer;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<UserEntity, RoleEntity, Guid, IdentityUserClaim<Guid>, UserRoleEntity,
        IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<PdfFileEntity> PdfFiles => Set<PdfFileEntity>();
    public DbSet<ShareLinkEntity> ShareLinks => Set<ShareLinkEntity>();
    public DbSet<ShareLinkUserEntity> ShareLinkUsers => Set<ShareLinkUserEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
