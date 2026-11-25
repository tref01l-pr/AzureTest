using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;

namespace PdfShare.DataAccess.SqlServer.Entities.SessionEntities;

public class SessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }

    public UserEntity User { get; set; } = null!;
}

public class SessionEntityConfiguration : IEntityTypeConfiguration<SessionEntity>
{
    public void Configure(EntityTypeBuilder<SessionEntity> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RefreshTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(s => s.RefreshTokenHash).IsUnique();
        builder.HasIndex(s => s.UserId);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
