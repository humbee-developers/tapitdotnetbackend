using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Spotlight.Queries;

public record GetCurrentSpotlightQuery : IRequest<Result<SpotlightSessionDto?>>;

public class GetCurrentSpotlightQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ISpotlightService spotlightService)
    : IRequestHandler<GetCurrentSpotlightQuery, Result<SpotlightSessionDto?>>
{
    public async Task<Result<SpotlightSessionDto?>> Handle(GetCurrentSpotlightQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var session = await uow.Repository<SpotlightSession>().Query()
            .Include(s => s.FeedItems)
            .Where(s => s.WatcherUserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (session is null)
        {
            var generated = await spotlightService.GenerateForUserAsync(userId, ct);
            if (generated is null)
                return Result<SpotlightSessionDto?>.Success(null);

            // Reload with FeedItems navigation property populated
            session = await uow.Repository<SpotlightSession>().Query()
                .Include(s => s.FeedItems)
                .FirstOrDefaultAsync(s => s.Id == generated.Id, ct);

            if (session is null)
                return Result<SpotlightSessionDto?>.Success(null);
        }

        var likedUserIds = await uow.Repository<UserLike>().Query()
            .Where(ul => ul.LikerId == currentUser.UserId)
            .Select(ul => ul.LikedUserId)
            .ToListAsync(ct);

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Where(p => session.FeedItems.Select(f => f.FeaturedUserId).Contains(p.UserId))
            .ToListAsync(ct);

        var dto = new SpotlightSessionDto
        {
            SessionId = session.Id,
            GeneratedAt = session.GeneratedAt,
            ExpiresAt = session.ExpiresAt,
            FeedItems = session.FeedItems.Select(fi =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == fi.FeaturedUserId);
                var photos = profile?.Photos;
                var primaryPhoto = photos?.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl
                    ?? (photos?.Count > 0 ? photos[0].PublicUrl : null);
                return new SpotlightFeedItemDto
                {
                    SpotlightSessionFeedId = fi.Id,
                    UserId = fi.FeaturedUserId,
                    DisplayName = profile?.DisplayName ?? "Unknown",
                    AgeRange = profile?.AgeRange ?? string.Empty,
                    Gender = profile?.Gender ?? string.Empty,
                    PhotoUrl = primaryPhoto,
                    HasLiked = likedUserIds.Contains(fi.FeaturedUserId),
                    CanSendConnectionRequest = likedUserIds.Contains(fi.FeaturedUserId),
                    ViewedAt = fi.ViewedAt,
                    LikedAt = fi.LikedAt
                };
            }).ToList()
        };

        return Result<SpotlightSessionDto?>.Success(dto);
    }

}
