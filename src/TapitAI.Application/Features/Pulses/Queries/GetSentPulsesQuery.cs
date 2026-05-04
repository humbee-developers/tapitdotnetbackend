using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Pulses.Queries;

public record PulseDto(
    Guid Id,
    string LikedUserId,
    string MaskedName,
    string? PlaceholderPhotoUrl,
    string AgeRange,
    DateTime LikedAt
);

public record GetSentPulsesQuery : IRequest<Result<List<PulseDto>>>;

public class GetSentPulsesQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetSentPulsesQuery, Result<List<PulseDto>>>
{
    public async Task<Result<List<PulseDto>>> Handle(GetSentPulsesQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var likes = await uow.Repository<UserLike>().Query()
            .Where(l => l.LikerId == userId)
            .OrderByDescending(l => l.LikedAt)
            .ToListAsync(ct);

        var likedUserIds = likes.Select(l => l.LikedUserId).Distinct().ToList();

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .Where(p => likedUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
            .Where(pp => pp.IsActive)
            .ToListAsync(ct);

        var dtos = likes.Select(l =>
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == l.LikedUserId);
            var gender = profile?.Gender ?? "MALE";
            var placeholder = GetPlaceholder(placeholders, gender);

            return new PulseDto(
                l.Id,
                l.LikedUserId,
                MaskName(profile?.DisplayName ?? "Someone"),
                placeholder,
                profile?.AgeRange ?? string.Empty,
                l.LikedAt
            );
        }).ToList();

        return Result<List<PulseDto>>.Success(dtos);
    }

    private static string GetPlaceholder(List<PlaceholderPhoto> photos, string gender)
    {
        var matches = photos.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!matches.Any()) matches = photos;
        if (!matches.Any()) return string.Empty;
        return matches[Random.Shared.Next(matches.Count)].PhotoUrl;
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
