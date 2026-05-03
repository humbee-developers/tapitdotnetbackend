namespace TapitAI.Domain.Interfaces.Services;

public record NearbyUserResult(
    string UserId,
    string MaskedName,
    string AgeRange,
    string SelfGender,
    string PlaceholderPhotoUrl,
    double DistanceMiles,
    bool CanSendConnectionRequest,
    Guid? ExistingConnectionId,
    string? ExistingConnectionStatus
);

public interface IDiscoveryService
{
    Task<IReadOnlyList<NearbyUserResult>> GetNearbyUsersAsync(
        string requestingUserId,
        double radiusMiles,
        CancellationToken cancellationToken = default);
}
