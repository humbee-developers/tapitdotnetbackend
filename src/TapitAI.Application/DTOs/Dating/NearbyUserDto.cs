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
    public Guid? ExistingConnectionId { get; set; }
    public string? ExistingConnectionStatus { get; set; }
}
