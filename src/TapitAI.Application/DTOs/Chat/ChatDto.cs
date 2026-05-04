namespace TapitAI.Application.DTOs.Chat;

public class ChatTokenDto
{
    public string StreamUserId { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class ChatChannelDto
{
    public Guid ConnectionId { get; set; }
    public string ChatChannelId { get; set; } = null!;
    public string OtherUserId { get; set; } = null!;
    public string OtherUserDisplayName { get; set; } = null!;
    public string? OtherUserPhotoUrl { get; set; }
    public string? OtherUserAgeRange { get; set; }
    public DateTime? ConnectedAt { get; set; }
}
