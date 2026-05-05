using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Spotlight.Queries;

public record GetSpotlightVisibilityQuery : IRequest<Result<SpotlightVisibilityDto>>;

public record SpotlightVisibilityDto(bool IsVisible);

public class GetSpotlightVisibilityQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetSpotlightVisibilityQuery, Result<SpotlightVisibilityDto>>
{
    public async Task<Result<SpotlightVisibilityDto>> Handle(GetSpotlightVisibilityQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var profile = await uow.Repository<UserDatingProfile>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var isVisible = profile?.IsSpotlightVisible ?? true;
        return Result<SpotlightVisibilityDto>.Success(new SpotlightVisibilityDto(isVisible));
    }
}
