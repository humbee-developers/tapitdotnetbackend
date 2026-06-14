using Microsoft.EntityFrameworkCore;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Common.Helpers;

public record ConnectionEventProfiles(
    string? SenderDisplayName,
    string? SenderPhotoUrl,
    string? SenderPlaceholderPhotoUrl,
    string? ReceiverDisplayName,
    string? ReceiverPhotoUrl,
    string? ReceiverPlaceholderPhotoUrl
);

public static class ConnectionEventPayload
{
    public static object Build(
        Connection c,
        string senderInternalId,
        string receiverInternalId,
        ConnectionEventProfiles? profiles = null)
        => new
        {
            ConnectionId              = c.Id,
            SenderUserId              = senderInternalId,
            ReceiverUserId            = receiverInternalId,
            InvitationStatus          = c.InvitationStatus.ToString(),
            SenderConnectionStatus    = c.SenderConnectionStatus?.ToString(),
            ReceiverConnectionStatus  = c.ReceiverConnectionStatus?.ToString(),
            SenderLocation            = new { Lat = c.SenderLocationLat,   Long = c.SenderLocationLong },
            ReceiverLocation          = new { Lat = c.ReceiverLocationLat, Long = c.ReceiverLocationLong },
            SenderInvitationMessage   = c.SenderInvitationMessage,
            ReceiverInvitationMessage = c.ReceiverInvitationMessage,
            SenderConnectionMessage   = c.SenderConnectionMessage,
            ReceiverConnectionMessage = c.ReceiverConnectionMessage,
            InitiatedVia              = c.InitiatedVia.ToString(),
            ChatChannelId             = c.ChatChannelId,
            InvitedAt                 = c.InvitedAt,
            AcceptedAt                = c.AcceptedAt,
            ConnectedAt               = c.ConnectedAt,
            ExpiresAt                 = c.ExpiresAt,
            // Identity fields — masked until accepted, real after accepted
            SenderMaskedName          = MaskName(profiles?.SenderDisplayName),
            SenderDisplayName         = profiles?.SenderDisplayName,
            SenderPhotoUrl            = profiles?.SenderPhotoUrl,
            SenderPlaceholderPhotoUrl = profiles?.SenderPlaceholderPhotoUrl,
            ReceiverMaskedName        = MaskName(profiles?.ReceiverDisplayName),
            ReceiverDisplayName       = profiles?.ReceiverDisplayName,
            ReceiverPhotoUrl          = profiles?.ReceiverPhotoUrl,
            ReceiverPlaceholderPhotoUrl = profiles?.ReceiverPlaceholderPhotoUrl
        };

    /// <summary>
    /// Loads sender + receiver profiles (with photos) and placeholder photos in 3 queries.
    /// Call this from Application-layer command handlers.
    /// </summary>
    public static async Task<ConnectionEventProfiles> LoadProfilesAsync(
        Connection connection,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var senderProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == connection.SenderUserId, ct);

        var receiverProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == connection.ReceiverUserId, ct);

        var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
            .Where(pp => pp.IsActive)
            .ToListAsync(ct);

        return new ConnectionEventProfiles(
            SenderDisplayName:          senderProfile?.DisplayName,
            SenderPhotoUrl:             senderProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            SenderPlaceholderPhotoUrl:  GetPlaceholder(placeholders, senderProfile?.Gender ?? "MALE", connection.SenderUserId),
            ReceiverDisplayName:        receiverProfile?.DisplayName,
            ReceiverPhotoUrl:           receiverProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            ReceiverPlaceholderPhotoUrl: GetPlaceholder(placeholders, receiverProfile?.Gender ?? "MALE", connection.ReceiverUserId)
        );
    }

    // "Jay Patel" → "Ja.. Pa.."  (first 2 chars of each word + "..")
    public static string? MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return string.Join(" ",
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => (w.Length >= 2 ? w[..2] : w) + ".."));
    }

    // Deterministic placeholder by gender + userId hash so it never flickers.
    public static string? GetPlaceholder(List<PlaceholderPhoto> placeholders, string gender, string userId)
    {
        var matches = placeholders.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) matches = placeholders;
        if (matches.Count == 0) return null;
        var hash = userId.Aggregate(0u, (acc, c) => acc * 31 + c);
        return matches[(int)(hash % (uint)matches.Count)].PhotoUrl;
    }
}
