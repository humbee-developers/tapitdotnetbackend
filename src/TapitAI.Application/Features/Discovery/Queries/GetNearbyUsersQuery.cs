using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Discovery.Queries;

public record GetNearbyUsersQuery(double? RadiusOverrideMiles = null) : IRequest<Result<DiscoveryResultDto>>;

public class GetNearbyUsersQueryHandler(
    ICurrentUserService currentUser,
    IDiscoveryService discoveryService,
    IAdminSettingService settings,
    IIdentityService identity,
    IUnitOfWork uow)
    : IRequestHandler<GetNearbyUsersQuery, Result<DiscoveryResultDto>>
{
    public async Task<Result<DiscoveryResultDto>> Handle(GetNearbyUsersQuery qry, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var radius = qry.RadiusOverrideMiles
            ?? await settings.GetDoubleAsync(AdminSettingKeys.DiscoveryRadiusMiles, 100, ct);

        var results = await discoveryService.GetNearbyUsersAsync(userId, radius, ct);

        // Find the other participant in any active connection (pending invitation or decision phase).
        var activeConn = await uow.Repository<Domain.Entities.Connection>().Query()
            .AsNoTracking()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && (c.InvitationStatus == InvitationStatus.Pending
                    || (c.InvitationStatus == InvitationStatus.Accepted
                        && c.ConnectedAt == null
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Expired
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Expired)))
            .Select(c => new { c.SenderUserId, c.ReceiverUserId })
            .FirstOrDefaultAsync(ct);

        var activeConnectionAuth0Id = activeConn is null ? null
            : activeConn.SenderUserId == userId
                ? activeConn.ReceiverUserId
                : activeConn.SenderUserId;

        // Batch-resolve all Auth0 subs to internal UUIDs in a single query.
        var auth0Ids = results.Select(r => r.UserId).ToList();
        if (activeConnectionAuth0Id is not null)
            auth0Ids.Add(activeConnectionAuth0Id);

        var idMap = await identity.ResolveInternalUserIdsAsync(auth0Ids, ct);

        var users = results.Select(r => new NearbyUserDto
        {
            UserId = idMap.GetValueOrDefault(r.UserId, r.UserId),
            MaskedName = r.MaskedName,
            AgeRange = r.AgeRange,
            SelfGender = r.SelfGender,
            PlaceholderPhotoUrl = r.PlaceholderPhotoUrl,
            DistanceMiles = r.DistanceMiles,
            CanSendConnectionRequest = r.CanSendConnectionRequest,
            CannotConnectReason = r.CannotConnectReason,
            ExistingConnectionId = r.ExistingConnectionId,
            ExistingConnectionStatus = r.ExistingConnectionStatus
        }).ToList();

        var activeConnectionUserId = activeConnectionAuth0Id is null ? null
            : idMap.GetValueOrDefault(activeConnectionAuth0Id, activeConnectionAuth0Id);

        return Result<DiscoveryResultDto>.Success(new DiscoveryResultDto
        {
            Users = users,
            ActiveConnectionUserId = activeConnectionUserId
        });
    }
}
