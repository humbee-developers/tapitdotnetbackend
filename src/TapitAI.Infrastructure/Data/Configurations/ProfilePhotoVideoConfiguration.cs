using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class ProfilePhotoConfiguration : IEntityTypeConfiguration<ProfilePhoto>
{
    public void Configure(EntityTypeBuilder<ProfilePhoto> builder)
    {
        builder.ToTable("ProfilePhotos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PublicUrl).IsRequired().HasMaxLength(1000);
        builder.HasIndex(p => new { p.UserDatingProfileId, p.IsPrimary });
    }
}

public class ProfileVideoConfiguration : IEntityTypeConfiguration<ProfileVideo>
{
    public void Configure(EntityTypeBuilder<ProfileVideo> builder)
    {
        builder.ToTable("ProfileVideos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.PublicUrl).IsRequired().HasMaxLength(1000);
        builder.HasIndex(v => v.UserDatingProfileId);
    }
}
