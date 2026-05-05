using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Spotlight.Commands;

public record UnlikeSpotlightUserCommand(Guid SpotlightSessionFeedId) : IRequest<Result>;

public class UnlikeSpotlightUserCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UnlikeSpotlightUserCommand, Result>
{
    public async Task<Result> Handle(UnlikeSpotlightUserCommand cmd, CancellationToken ct)
    {
        var feedItem = await uow.Repository<SpotlightSessionFeed>().GetByIdAsync(cmd.SpotlightSessionFeedId, ct)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(SpotlightSessionFeed), cmd.SpotlightSessionFeedId);

        var like = await uow.Repository<UserLike>().Query()
            .FirstOrDefaultAsync(ul => ul.LikerId == currentUser.UserId && ul.LikedUserId == feedItem.FeaturedUserId, ct);

        if (like is not null)
            uow.Repository<UserLike>().Remove(like);

        feedItem.Unlike();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
