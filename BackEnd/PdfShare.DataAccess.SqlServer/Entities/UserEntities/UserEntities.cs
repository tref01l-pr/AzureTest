using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfShare.DataAccess.SqlServer.Entities.FileEntities;
using PdfShare.DataAccess.SqlServer.Entities.SessionEntities;

namespace PdfShare.DataAccess.SqlServer.Entities.UserEntities;

public class UserEntity : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
    public ICollection<PdfFileEntity> Files { get; set; } = [];
    public ICollection<SessionEntity> Sessions { get; set; } = [];
}

public class RoleEntity : IdentityRole<Guid>
{
    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
}

public class UserRoleEntity : IdentityUserRole<Guid>
{
    public UserEntity User { get; set; } = null!;
    public RoleEntity Role { get; set; } = null!;
}

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();
    }
}

public class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();
    }
}
