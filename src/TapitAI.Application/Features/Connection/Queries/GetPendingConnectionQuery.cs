using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Connection.Queries;

public record GetPendingConnectionQuery : IRequest<Result<ConnectionDetailDto?>>;

public class GetPendingConnectionQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetPendingConnectionQuery, Result<ConnectionDetailDto?>>
{
    public async Task<Result<ConnectionDetailDto?>> Handle(GetPendingConnectionQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        // Return the active decision-phase connection for this user.
        // Visible to both participants until either passes or either side's timer expires.
        // MyConnectionStatus/PartnerConnectionStatus tells the mobile whether to show
        // "decide now" (Pending) or "waiting for partner" (Connect) UI.
        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                c.InvitationStatus == InvitationStatus.Accepted
                && c.ConnectedAt == null
                && c.SenderConnectionStatus != ParticipantConnectionStatus.Pass
                && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pass
                && c.SenderConnectionStatus != ParticipantConnectionStatus.Expired
                && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Expired
                && (c.SenderUserId == userId || c.ReceiverUserId == userId))
            .OrderByDescending(c => c.AcceptedAt)
            .FirstOrDefaultAsync(ct);

        if (connection is null)
            return Result<ConnectionDetailDto?>.Success(null);

        var isSender = connection.SenderUserId == userId;
        var otherUserId = isSender ? connection.ReceiverUserId : connection.SenderUserId;

        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == otherUserId, ct);

        var myStatus = isSender ? connection.SenderConnectionStatus : connection.ReceiverConnectionStatus;
        var partnerStatus = isSender ? connection.ReceiverConnectionStatus : connection.SenderConnectionStatus;

        // System connections keep identity hidden until both participants connect (ConnectedAt is set).
        var hideIdentity = connection.InitiatedVia == ConnectionInitiatedVia.System
                           && connection.ConnectedAt == null;

        string? photoUrl = null;
        if (hideIdentity)
        {
            var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
                .Where(pp => pp.IsActive).ToListAsync(ct);
            photoUrl = GetPlaceholder(placeholders, profile?.Gender ?? "MALE", connection.Id);
        }
        else
        {
            photoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl;
        }

        return Result<ConnectionDetailDto?>.Success(new ConnectionDetailDto
        {
            ConnectionId = connection.Id,
            OtherUserId = otherUserId,
            OtherUserDisplayName = hideIdentity ? MaskName(profile?.DisplayName ?? "Someone") : (profile?.DisplayName ?? "Unknown"),
            OtherUserPrimaryPhotoUrl = photoUrl,
            OtherUserAgeRange = profile?.AgeRange ?? string.Empty,
            InvitationStatus = connection.InvitationStatus.ToString(),
            MyConnectionStatus = myStatus?.ToString(),
            PartnerConnectionStatus = partnerStatus?.ToString(),
            ChatChannelId = connection.ChatChannelId,
            InvitedAt = connection.InvitedAt,
            ConnectedAt = connection.ConnectedAt,
            ExpiresAt = connection.ExpiresAt,
            InitiatedVia = connection.InitiatedVia.ToString(),
            IsIdentityHidden = hideIdentity,
            IsSender = isSender
        });
    }

    private static string GetPlaceholder(List<PlaceholderPhoto> placeholders, string gender, Guid connectionId)
    {
        var matches = placeholders.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) matches = placeholders;
        if (matches.Count == 0) return string.Empty;
        var index = (int)(BitConverter.ToUInt32(connectionId.ToByteArray(), 0) % (uint)matches.Count);
        return matches[index].PhotoUrl;
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var chars = name.ToCharArray();
        for (var i = 1; i < chars.Length; i += 2)
            if (chars[i] != ' ') chars[i] = '*';
        return new string(chars);
    }
}
