using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class SpotlightSession : AuditableEntity
{
    public string WatcherUserId { get; private set; } = null!;
    public DateTime GeneratedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<SpotlightSessionFeed> _feedItems = new();
    public IReadOnlyList<SpotlightSessionFeed> FeedItems => _feedItems.AsReadOnly();

    private SpotlightSession() { }

    public static SpotlightSession Create(string watcherUserId, int expiryMinutes)
    {
        var now = DateTime.UtcNow;
        return new()
        {
            WatcherUserId = watcherUserId,
            GeneratedAt = now,
            ExpiresAt = now.AddMinutes(expiryMinutes),
            IsActive = true
        };
    }

    public void AddFeedItem(SpotlightSessionFeed item) => _feedItems.Add(item);
    public void Expire() => IsActive = false;
    public bool HasExpired() => DateTime.UtcNow > ExpiresAt;
}
