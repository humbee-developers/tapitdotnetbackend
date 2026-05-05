using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Chat;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Chat.Queries;

public record GetChatChannelsQuery : IRequest<Result<List<ChatChannelDto>>>;

public class GetChatChannelsQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser)
    : IRequestHandler<GetChatChannelsQuery, Result<List<ChatChannelDto>>>
{
    public async Task<Result<List<ChatChannelDto>>> Handle(GetChatChannelsQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var blockedUserIds = await uow.Repository<UserBlock>().Query()
            .AsNoTracking()
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId)
            .ToHashSetAsync(ct);

        var connections = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && c.ChatChannelId != null
                && c.SenderConnectionStatus == ParticipantConnectionStatus.Connect
                && c.ReceiverConnectionStatus == ParticipantConnectionStatus.Connect
                && !blockedUserIds.Contains(c.SenderUserId)
                && !blockedUserIds.Contains(c.ReceiverUserId))
            .OrderByDescending(c => c.ConnectedAt)
            .ToListAsync(ct);

        var otherUserIds = connections
            .Select(c => c.SenderUserId == userId ? c.ReceiverUserId : c.SenderUserId)
            .Distinct()
            .ToList();

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var profileMap = profiles.ToDictionary(p => p.UserId);

        var result = connections.Select(c =>
        {
            var otherUserId = c.SenderUserId == userId ? c.ReceiverUserId : c.SenderUserId;
            profileMap.TryGetValue(otherUserId, out var profile);

            return new ChatChannelDto
            {
                ConnectionId = c.Id,
                ChatChannelId = c.ChatChannelId!,
                OtherUserId = otherUserId,
                OtherUserDisplayName = profile?.DisplayName ?? "Unknown",
                OtherUserPhotoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
                OtherUserAgeRange = profile?.AgeRange,
                ConnectedAt = c.ConnectedAt
            };
        }).ToList();

        return Result<List<ChatChannelDto>>.Success(result);
    }
}
