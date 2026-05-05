using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class SpotlightSessionFeed : BaseEntity
{
    public Guid SpotlightSessionId { get; private set; }
    public string FeaturedUserId { get; private set; } = null!;
    public DateTime? ViewedAt { get; private set; }
    public DateTime? LikedAt { get; private set; }

    private SpotlightSessionFeed() { }

    public static SpotlightSessionFeed Create(Guid sessionId, string featuredUserId)
        => new() { SpotlightSessionId = sessionId, FeaturedUserId = featuredUserId };

    public void MarkViewed()
    {
        if (ViewedAt is null) ViewedAt = DateTime.UtcNow;
    }

    public void MarkLiked()
    {
        LikedAt = DateTime.UtcNow;
        MarkViewed();
    }

    public void Unlike() => LikedAt = null;
}
