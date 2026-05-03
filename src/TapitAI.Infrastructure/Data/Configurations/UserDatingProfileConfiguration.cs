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
        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.AgeRangeOption)
            .WithMany().HasForeignKey(p => p.AgeRangeOptionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SelfGenderOption)
            .WithMany().HasForeignKey(p => p.SelfGenderOptionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PreferHeightOption)
            .WithMany().HasForeignKey(p => p.PreferHeightOptionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Photos)
            .WithOne().HasForeignKey(ph => ph.UserDatingProfileId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Videos)
            .WithOne().HasForeignKey(v => v.UserDatingProfileId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.InterestedGenders)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserProfileInterestedGenders"));

        builder.HasMany(p => p.Lifestyles)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserProfileLifestyles"));

        builder.HasMany(p => p.LookingFors)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserProfileLookingFors"));
    }
}
