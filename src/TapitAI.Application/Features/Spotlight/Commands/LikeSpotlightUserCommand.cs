using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Spotlight.Commands;

public record LikeSpotlightUserCommand(Guid SpotlightSessionFeedId) : IRequest<Result>;

public class LikeSpotlightUserCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<LikeSpotlightUserCommand, Result>
{
    public async Task<Result> Handle(LikeSpotlightUserCommand cmd, CancellationToken ct)
    {
        var feedItem = await uow.Repository<SpotlightSessionFeed>().GetByIdAsync(cmd.SpotlightSessionFeedId, ct)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(SpotlightSessionFeed), cmd.SpotlightSessionFeedId);

        var alreadyLiked = await uow.Repository<UserLike>().Query()
            .AnyAsync(ul => ul.LikerId == currentUser.UserId && ul.LikedUserId == feedItem.FeaturedUserId, ct);

        if (!alreadyLiked)
        {
            var like = UserLike.Create(currentUser.UserId!, feedItem.FeaturedUserId, cmd.SpotlightSessionFeedId);
            await uow.Repository<UserLike>().AddAsync(like, ct);
        }

        feedItem.MarkLiked();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
