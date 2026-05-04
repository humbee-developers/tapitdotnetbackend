using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Connection.Queries;

public record GetConnectionsQuery : IRequest<Result<List<ConnectionDetailDto>>>;

public class GetConnectionsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetConnectionsQuery, Result<List<ConnectionDetailDto>>>
{
    public async Task<Result<List<ConnectionDetailDto>>> Handle(GetConnectionsQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var connections = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && c.InvitationStatus == InvitationStatus.Accepted)
            .OrderByDescending(c => c.AcceptedAt)
            .ToListAsync(ct);

        var otherUserIds = connections
            .Select(c => c.SenderUserId == userId ? c.ReceiverUserId : c.SenderUserId)
            .Distinct().ToList();

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var dtos = connections.Select(c =>
        {
            var isSender = c.SenderUserId == userId;
            var otherUserId = isSender ? c.ReceiverUserId : c.SenderUserId;
            var profile = profiles.FirstOrDefault(p => p.UserId == otherUserId);
            var myStatus = isSender ? c.SenderConnectionStatus : c.ReceiverConnectionStatus;
            var partnerStatus = isSender ? c.ReceiverConnectionStatus : c.SenderConnectionStatus;

            return new ConnectionDetailDto
            {
                ConnectionId = c.Id,
                OtherUserId = otherUserId,
                OtherUserDisplayName = profile?.DisplayName ?? "Unknown",
                OtherUserPrimaryPhotoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
                OtherUserAgeRange = profile?.AgeRange ?? string.Empty,
                InvitationStatus = c.InvitationStatus.ToString(),
                MyConnectionStatus = myStatus?.ToString(),
                PartnerConnectionStatus = partnerStatus?.ToString(),
                ChatChannelId = c.ChatChannelId,
                InvitedAt = c.InvitedAt,
                ConnectedAt = c.ConnectedAt,
                IsSender = isSender
            };
        }).ToList();

        return Result<List<ConnectionDetailDto>>.Success(dtos);
    }
}
