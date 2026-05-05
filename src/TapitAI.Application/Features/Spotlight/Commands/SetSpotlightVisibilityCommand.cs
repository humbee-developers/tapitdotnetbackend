using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Spotlight.Commands;

public record SetSpotlightVisibilityCommand(bool IsVisible) : IRequest<Result>;

public class SetSpotlightVisibilityCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<SetSpotlightVisibilityCommand, Result>
{
    public async Task<Result> Handle(SetSpotlightVisibilityCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var profile = await uow.Repository<UserDatingProfile>().Query()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
            return Result.Failure("Profile not found.");

        profile.SetSpotlightVisibility(cmd.IsVisible);
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
