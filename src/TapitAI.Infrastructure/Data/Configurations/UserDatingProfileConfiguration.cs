using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserDatingProfileConfiguration : IEntityTypeConfiguration<UserDatingProfile>
{
    public void Configure(EntityTypeBuilder<UserDatingProfile> builder)
    {
        builder.ToTable("UserDatingProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Gender).IsRequired().HasMaxLength(50);
        builder.Property(p => p.AgeRange).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Bio).HasMaxLength(1000);

        builder.Property(p => p.GenderPreference).HasColumnType("text[]");
        builder.Property(p => p.HeightPreference).HasColumnType("text[]");
        builder.Property(p => p.Lifestyle).HasColumnType("text[]");
        builder.Property(p => p.LookingFor).HasColumnType("text[]");

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasMany(p => p.Photos)
            .WithOne().HasForeignKey(ph => ph.UserDatingProfileId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Videos)
            .WithOne().HasForeignKey(v => v.UserDatingProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
