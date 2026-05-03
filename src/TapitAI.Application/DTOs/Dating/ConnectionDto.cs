namespace TapitAI.Application.DTOs.Dating;

public class ConnectionInvitationDto
{
    public Guid ConnectionId { get; set; }
    public string OtherUserMaskedName { get; set; } = null!;
    public string OtherUserPlaceholderPhotoUrl { get; set; } = null!;
    public string OtherUserAgeRange { get; set; } = null!;
    public string? InvitationMessage { get; set; }
    public string InitiatedVia { get; set; } = null!;
    public DateTime InvitedAt { get; set; }
    public string Status { get; set; } = null!;
    public bool IsSender { get; set; }
}

public class ConnectionDetailDto
{
    public Guid ConnectionId { get; set; }
    public string OtherUserId { get; set; } = null!;
    public string OtherUserDisplayName { get; set; } = null!;
    public string? OtherUserPrimaryPhotoUrl { get; set; }
    public string OtherUserAgeRange { get; set; } = null!;
    public string InvitationStatus { get; set; } = null!;
    public string? MyConnectionStatus { get; set; }
    public string? PartnerConnectionStatus { get; set; }
    public string? ChatChannelId { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public bool IsSender { get; set; }
}

public class ConnectionActionResultDto
{
    public Guid ConnectionId { get; set; }
    public string Status { get; set; } = null!;
    public string? ChatChannelId { get; set; }
    public bool BothConnected { get; set; }
    public string? Message { get; set; }
}
