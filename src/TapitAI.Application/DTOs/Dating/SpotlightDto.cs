namespace TapitAI.Application.DTOs.Dating;

public class SpotlightSessionDto
{
    public Guid SessionId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<SpotlightFeedItemDto> FeedItems { get; set; } = new();
}

public class SpotlightFeedItemDto
{
    public Guid SpotlightSessionFeedId { get; set; }
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string AgeRange { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public double DistanceMiles { get; set; }
    public bool HasLiked { get; set; }
    public bool CanSendConnectionRequest { get; set; }
    public Guid? ExistingConnectionId { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? LikedAt { get; set; }
}
