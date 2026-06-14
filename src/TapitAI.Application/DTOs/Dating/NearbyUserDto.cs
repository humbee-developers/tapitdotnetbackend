namespace TapitAI.Application.DTOs.Dating;

public class NearbyUserDto
{
    public string UserId { get; set; } = null!;
    public string MaskedName { get; set; } = null!;
    public string AgeRange { get; set; } = null!;
    public string SelfGender { get; set; } = null!;
    public string PlaceholderPhotoUrl { get; set; } = null!;
    public double DistanceMiles { get; set; }
    public bool CanSendConnectionRequest { get; set; }
    public string? CannotConnectReason { get; set; }
    public Guid? ExistingConnectionId { get; set; }
    public string? ExistingConnectionStatus { get; set; }
}

public class DiscoveryResultDto
{
    /// <summary>
    /// Nearby users visible on the map.
    /// </summary>
    public List<NearbyUserDto> Users { get; set; } = [];

    /// <summary>
    /// The user ID of the person the requesting user is currently in an active
    /// connection with (any stage: pending invitation, decision phase).
    /// Null when there is no active connection. Use this to draw the map line.
    /// </summary>
    public string? ActiveConnectionUserId { get; set; }
}
