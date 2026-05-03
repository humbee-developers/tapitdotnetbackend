using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
{
    public void Configure(EntityTypeBuilder<UserLocation> builder)
    {
        builder.ToTable("UserLocations");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.UserId).IsRequired().HasMaxLength(450);
        builder.Property(l => l.Location).HasColumnType("geography(Point, 4326)").IsRequired();
        builder.HasIndex(l => new { l.UserId, l.IsLatest });
        builder.HasIndex(l => l.Location).HasMethod("GIST");
    }
}
