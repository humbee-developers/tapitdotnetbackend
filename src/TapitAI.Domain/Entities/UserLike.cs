using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserLike : BaseEntity
{
    public string LikerId { get; private set; } = null!;
    public string LikedUserId { get; private set; } = null!;
    public Guid? SpotlightSessionFeedId { get; private set; }
    public DateTime LikedAt { get; private set; }

    private UserLike() { }

    public static UserLike Create(string likerId, string likedUserId, Guid? spotlightSessionFeedId = null)
        => new()
        {
            LikerId = likerId,
            LikedUserId = likedUserId,
            SpotlightSessionFeedId = spotlightSessionFeedId,
            LikedAt = DateTime.UtcNow
        };
}
