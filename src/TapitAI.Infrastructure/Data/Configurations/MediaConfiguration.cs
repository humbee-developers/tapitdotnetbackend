using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FileName).IsRequired().HasMaxLength(255);
        builder.Property(m => m.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(m => m.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(m => m.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(m => m.PublicUrl).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.UploadedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Folder).HasMaxLength(255);

        builder.HasIndex(m => m.UploadedByUserId);
        builder.HasIndex(m => m.StorageKey).IsUnique();
        builder.HasIndex(m => new { m.IsDeleted, m.CreatedAt });
    }
}
