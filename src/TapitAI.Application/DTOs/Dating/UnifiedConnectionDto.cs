namespace TapitAI.Application.DTOs.Dating;

public class UnifiedConnectionDto
{
    public Guid ConnectionId { get; set; }
    public string SenderUserId { get; set; } = null!;
    public string ReceiverUserId { get; set; } = null!;
    public string InvitationStatus { get; set; } = null!;
    public string? SenderConnectionStatus { get; set; }
    public string? ReceiverConnectionStatus { get; set; }
    public LocationDto SenderLocation { get; set; } = null!;
    public LocationDto ReceiverLocation { get; set; } = null!;
    public string? SenderInvitationMessage { get; set; }
    public string? ReceiverInvitationMessage { get; set; }
    public string? SenderConnectionMessage { get; set; }
    public string? ReceiverConnectionMessage { get; set; }
    public string InitiatedVia { get; set; } = null!;
    public string? ChatChannelId { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class LocationDto
{
    public double Lat { get; set; }
    public double Long { get; set; }
}
