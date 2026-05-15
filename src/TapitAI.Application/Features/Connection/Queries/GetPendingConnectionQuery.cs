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
        var userId = currentUser.UserId;

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

        // Decision phase always reveals real identity — invitation was accepted by the receiver.
        var photoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl;

        return Result<ConnectionDetailDto?>.Success(new ConnectionDetailDto
        {
            ConnectionId = connection.Id,
            OtherUserId = otherUserId,
            OtherUserDisplayName = profile?.DisplayName ?? "Unknown",
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
            IsIdentityHidden = false,
            IsSender = isSender
        });
    }

}
