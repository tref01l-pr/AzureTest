using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfShare.DataAccess.SqlServer.Entities.ShareLinkEntities;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.Domain;
using PdfShare.Domain.Constants;

namespace PdfShare.DataAccess.SqlServer.Entities.FileEntities;

public class PdfFileEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public FileStatus Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public int? PageCount { get; set; }
    public string? ThumbnailBlobName { get; set; }
    public string? RejectReason { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }

    public UserEntity Owner { get; set; } = null!;
    public ICollection<ShareLinkEntity> ShareLinks { get; set; } = [];
}

public class PdfFileEntityConfiguration : IEntityTypeConfiguration<PdfFileEntity>
{
    public void Configure(EntityTypeBuilder<PdfFileEntity> builder)
    {
        builder.ToTable("PdfFiles");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginalFileName).HasMaxLength(FileConstraints.MaxFileNameLength).IsRequired();
        builder.Property(f => f.BlobName).HasMaxLength(300).IsRequired();
        builder.Property(f => f.ThumbnailBlobName).HasMaxLength(300);
        builder.Property(f => f.RejectReason).HasMaxLength(500);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(f => f.BlobName).IsUnique();
        builder.HasIndex(f => new { f.OwnerId, f.UploadedAt });

        builder.HasOne(f => f.Owner)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
