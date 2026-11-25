using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfShare.DataAccess.SqlServer.Entities.FileEntities;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.Domain;

namespace PdfShare.DataAccess.SqlServer.Entities.ShareLinkEntities;

public class ShareLinkEntity
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public string Token { get; set; } = string.Empty;
    public ShareLinkType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public PdfFileEntity File { get; set; } = null!;
    public ICollection<ShareLinkUserEntity> Users { get; set; } = [];
}

public class ShareLinkUserEntity
{
    public Guid ShareLinkId { get; set; }
    public Guid UserId { get; set; }

    public ShareLinkEntity ShareLink { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}

public class ShareLinkEntityConfiguration : IEntityTypeConfiguration<ShareLinkEntity>
{
    public void Configure(EntityTypeBuilder<ShareLinkEntity> builder)
    {
        builder.ToTable("ShareLinks");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Token).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(l => l.Token).IsUnique();

        builder.HasOne(l => l.File)
            .WithMany(f => f.ShareLinks)
            .HasForeignKey(l => l.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShareLinkUserEntityConfiguration : IEntityTypeConfiguration<ShareLinkUserEntity>
{
    public void Configure(EntityTypeBuilder<ShareLinkUserEntity> builder)
    {
        builder.ToTable("ShareLinkUsers");
        builder.HasKey(x => new { x.ShareLinkId, x.UserId });
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.ShareLink)
            .WithMany(l => l.Users)
            .HasForeignKey(x => x.ShareLinkId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction: SQL Server forbids a second cascade path to ShareLinkUsers.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
