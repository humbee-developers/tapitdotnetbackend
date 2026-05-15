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

        // An accepted connection where THIS user specifically hasn't decided chat/pass yet.
        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                c.InvitationStatus == InvitationStatus.Accepted
                && ((c.SenderUserId == userId && c.SenderConnectionStatus == ParticipantConnectionStatus.Pending)
                 || (c.ReceiverUserId == userId && c.ReceiverConnectionStatus == ParticipantConnectionStatus.Pending)))
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

        return Result<ConnectionDetailDto?>.Success(new ConnectionDetailDto
        {
            ConnectionId = connection.Id,
            OtherUserId = otherUserId,
            OtherUserDisplayName = profile?.DisplayName ?? "Unknown",
            OtherUserPrimaryPhotoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange = profile?.AgeRange ?? string.Empty,
            InvitationStatus = connection.InvitationStatus.ToString(),
            MyConnectionStatus = myStatus?.ToString(),
            PartnerConnectionStatus = partnerStatus?.ToString(),
            ChatChannelId = connection.ChatChannelId,
            InvitedAt = connection.InvitedAt,
            ConnectedAt = connection.ConnectedAt,
            IsSender = isSender
        });
    }
}
