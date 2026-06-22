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
    /// <summary>
    /// Builds the unified event payload.
    /// <para>
    /// When <paramref name="viewerInternalId"/> is provided the payload also includes
    /// pre-resolved <c>OtherUser*</c> convenience fields so the mobile never has to
    /// do role detection — it just reads <c>otherUserDisplayName</c>, etc.
    /// </para>
    /// </summary>
    public static object Build(
        Connection c,
        string senderInternalId,
        string receiverInternalId,
        ConnectionEventProfiles? profiles = null,
        string? viewerInternalId = null)
    {
        var viewerIsSender = viewerInternalId == null || viewerInternalId == senderInternalId;

        var otherDisplayName        = viewerIsSender ? profiles?.ReceiverDisplayName         : profiles?.SenderDisplayName;
        var otherPhotoUrl           = viewerIsSender ? profiles?.ReceiverPhotoUrl            : profiles?.SenderPhotoUrl;
        var otherMaskedName         = viewerIsSender ? MaskName(profiles?.ReceiverDisplayName) : MaskName(profiles?.SenderDisplayName);
        var otherPlaceholderPhotoUrl = viewerIsSender ? profiles?.ReceiverPlaceholderPhotoUrl : profiles?.SenderPlaceholderPhotoUrl;
        var otherUserId             = viewerIsSender ? receiverInternalId                    : senderInternalId;

        return new
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
            // Per-user identity fields (sender / receiver perspective)
            SenderMaskedName            = MaskName(profiles?.SenderDisplayName),
            SenderDisplayName           = profiles?.SenderDisplayName,
            SenderPhotoUrl              = profiles?.SenderPhotoUrl,
            SenderPlaceholderPhotoUrl   = profiles?.SenderPlaceholderPhotoUrl,
            ReceiverMaskedName          = MaskName(profiles?.ReceiverDisplayName),
            ReceiverDisplayName         = profiles?.ReceiverDisplayName,
            ReceiverPhotoUrl            = profiles?.ReceiverPhotoUrl,
            ReceiverPlaceholderPhotoUrl = profiles?.ReceiverPlaceholderPhotoUrl,
            // Pre-resolved "other user" fields — relative to the specific recipient.
            // The mobile reads these directly without needing to compare UUIDs.
            OtherUserId              = otherUserId,
            OtherUserDisplayName     = otherDisplayName,
            OtherUserMaskedName      = otherMaskedName,
            OtherUserPhotoUrl        = otherPhotoUrl,
            OtherUserPlaceholderPhotoUrl = otherPlaceholderPhotoUrl
        };
    }

    /// <summary>
    /// Loads sender + receiver profiles (with photos) and placeholder photos in 3 queries.
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
            SenderDisplayName:           senderProfile?.DisplayName,
            SenderPhotoUrl:              senderProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            SenderPlaceholderPhotoUrl:   GetPlaceholder(placeholders, senderProfile?.Gender ?? "MALE",   connection.SenderUserId),
            ReceiverDisplayName:         receiverProfile?.DisplayName,
            ReceiverPhotoUrl:            receiverProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
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

    // Deterministic placeholder by gender + userId hash — never flickers.
    public static string? GetPlaceholder(List<PlaceholderPhoto> placeholders, string gender, string userId)
    {
        var matches = placeholders.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) matches = placeholders;
        if (matches.Count == 0) return null;
        var hash = userId.Aggregate(0u, (acc, c) => acc * 31 + c);
        return matches[(int)(hash % (uint)matches.Count)].PhotoUrl;
    }
}
