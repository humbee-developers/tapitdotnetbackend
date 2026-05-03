using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TapitAI.Domain.Entities;

namespace TapitAI.Infrastructure.Data.Configurations;

public class SpotlightSessionConfiguration : IEntityTypeConfiguration<SpotlightSession>
{
    public void Configure(EntityTypeBuilder<SpotlightSession> builder)
    {
        builder.ToTable("SpotlightSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.WatcherUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(s => new { s.WatcherUserId, s.IsActive });

        builder.HasMany(s => s.FeedItems)
            .WithOne().HasForeignKey(fi => fi.SpotlightSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpotlightSessionFeedConfiguration : IEntityTypeConfiguration<SpotlightSessionFeed>
{
    public void Configure(EntityTypeBuilder<SpotlightSessionFeed> builder)
    {
        builder.ToTable("SpotlightSessionFeeds");
        builder.HasKey(fi => fi.Id);
        builder.Property(fi => fi.FeaturedUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(fi => new { fi.SpotlightSessionId, fi.FeaturedUserId });
    }
}
