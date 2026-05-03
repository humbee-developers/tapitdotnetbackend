using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class PlaceholderPhotoConfiguration : IEntityTypeConfiguration<PlaceholderPhoto>
{
    public void Configure(EntityTypeBuilder<PlaceholderPhoto> builder)
    {
        builder.ToTable("PlaceholderPhotos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Gender).IsRequired().HasMaxLength(50);
        builder.Property(p => p.PhotoUrl).IsRequired().HasMaxLength(500);
        builder.HasIndex(p => new { p.Gender, p.IsActive });
    }
}
