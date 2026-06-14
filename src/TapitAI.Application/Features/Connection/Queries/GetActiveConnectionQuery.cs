using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Connection.Queries;

public record GetActiveConnectionQuery : IRequest<Result<UnifiedConnectionDto?>>;

public class GetActiveConnectionQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IIdentityService identity)
    : IRequestHandler<GetActiveConnectionQuery, Result<UnifiedConnectionDto?>>
{
    public async Task<Result<UnifiedConnectionDto?>> Handle(GetActiveConnectionQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && (c.InvitationStatus == InvitationStatus.Pending
                    || (c.InvitationStatus == InvitationStatus.Accepted
                        && c.ConnectedAt == null
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Expired
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Expired)))
            .OrderByDescending(c => c.InvitedAt)
            .FirstOrDefaultAsync(ct);

        if (connection is null)
            return Result<UnifiedConnectionDto?>.Success(null);

        // Resolve Auth0 subs to internal UUIDs so the mobile can match against its stored user ID.
        var senderInternalId   = await identity.ResolveInternalUserIdAsync(connection.SenderUserId, ct)   ?? connection.SenderUserId;
        var receiverInternalId = await identity.ResolveInternalUserIdAsync(connection.ReceiverUserId, ct) ?? connection.ReceiverUserId;

        return Result<UnifiedConnectionDto?>.Success(new UnifiedConnectionDto
        {
            ConnectionId              = connection.Id,
            SenderUserId              = senderInternalId,
            ReceiverUserId            = receiverInternalId,
            InvitationStatus          = connection.InvitationStatus.ToString(),
            SenderConnectionStatus    = connection.SenderConnectionStatus?.ToString(),
            ReceiverConnectionStatus  = connection.ReceiverConnectionStatus?.ToString(),
            SenderLocation            = new LocationDto { Lat = connection.SenderLocationLat,  Long = connection.SenderLocationLong },
            ReceiverLocation          = new LocationDto { Lat = connection.ReceiverLocationLat, Long = connection.ReceiverLocationLong },
            SenderInvitationMessage   = connection.SenderInvitationMessage,
            ReceiverInvitationMessage = connection.ReceiverInvitationMessage,
            SenderConnectionMessage   = connection.SenderConnectionMessage,
            ReceiverConnectionMessage = connection.ReceiverConnectionMessage,
            InitiatedVia              = connection.InitiatedVia.ToString(),
            ChatChannelId             = connection.ChatChannelId,
            InvitedAt                 = connection.InvitedAt,
            AcceptedAt                = connection.AcceptedAt,
            ConnectedAt               = connection.ConnectedAt,
            ExpiresAt                 = connection.ExpiresAt
        });
    }
}
