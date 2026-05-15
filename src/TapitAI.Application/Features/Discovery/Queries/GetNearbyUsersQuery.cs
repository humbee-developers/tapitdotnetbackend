using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Discovery.Queries;

public record GetNearbyUsersQuery(double? RadiusOverrideMiles = null) : IRequest<Result<List<NearbyUserDto>>>;

public class GetNearbyUsersQueryHandler(
    ICurrentUserService currentUser,
    IDiscoveryService discoveryService,
    IAdminSettingService settings)
    : IRequestHandler<GetNearbyUsersQuery, Result<List<NearbyUserDto>>>
{
    public async Task<Result<List<NearbyUserDto>>> Handle(GetNearbyUsersQuery qry, CancellationToken ct)
    {
        var radius = qry.RadiusOverrideMiles
            ?? await settings.GetDoubleAsync(AdminSettingKeys.DiscoveryRadiusMiles, 100, ct);

        var results = await discoveryService.GetNearbyUsersAsync(currentUser.UserId!, radius, ct);

        var dtos = results.Select(r => new NearbyUserDto
        {
            UserId = r.UserId,
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

        return Result<List<NearbyUserDto>>.Success(dtos);
    }
}
