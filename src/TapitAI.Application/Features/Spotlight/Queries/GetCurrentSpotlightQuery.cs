using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Spotlight.Queries;

public record GetCurrentSpotlightQuery : IRequest<Result<SpotlightSessionDto?>>;

public class GetCurrentSpotlightQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IAdminSettingService settings)
    : IRequestHandler<GetCurrentSpotlightQuery, Result<SpotlightSessionDto?>>
{
    public async Task<Result<SpotlightSessionDto?>> Handle(GetCurrentSpotlightQuery _, CancellationToken ct)
    {
        var session = await uow.Repository<SpotlightSession>().Query()
            .Include(s => s.FeedItems)
            .Where(s => s.WatcherUserId == currentUser.UserId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Result<SpotlightSessionDto?>.Success(null);

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
                return new SpotlightFeedItemDto
                {
                    SpotlightSessionFeedId = fi.Id,
                    UserId = fi.FeaturedUserId,
                    MaskedName = MaskName(profile?.DisplayName ?? "Unknown"),
                    AgeRange = profile?.AgeRange ?? string.Empty,
                    SelfGender = profile?.Gender ?? string.Empty,
                    PlaceholderPhotoUrl = string.Empty, // filled by discovery service in background gen
                    HasLiked = likedUserIds.Contains(fi.FeaturedUserId),
                    CanSendConnectionRequest = likedUserIds.Contains(fi.FeaturedUserId),
                    ViewedAt = fi.ViewedAt,
                    LikedAt = fi.LikedAt
                };
            }).ToList()
        };

        return Result<SpotlightSessionDto?>.Success(dto);
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var chars = name.ToCharArray();
        for (var i = 1; i < chars.Length; i += 2)
            if (chars[i] != ' ') chars[i] = '*';
        return new string(chars);
    }
}
