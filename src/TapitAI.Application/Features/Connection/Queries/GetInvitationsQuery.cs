using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Connection.Queries;

public record GetInvitationsQuery : IRequest<Result<List<ConnectionInvitationDto>>>;

public class GetInvitationsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetInvitationsQuery, Result<List<ConnectionInvitationDto>>>
{
    public async Task<Result<List<ConnectionInvitationDto>>> Handle(GetInvitationsQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var connections = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && (c.InvitationStatus == InvitationStatus.Pending || c.InvitationStatus == InvitationStatus.Rejected))
            .OrderByDescending(c => c.InvitedAt)
            .ToListAsync(ct);

        var otherUserIds = connections
            .Select(c => c.SenderUserId == userId ? c.ReceiverUserId : c.SenderUserId)
            .Distinct().ToList();

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
            .Where(pp => pp.IsActive).ToListAsync(ct);

        var dtos = connections.Select(c =>
        {
            var isSender = c.SenderUserId == userId;
            var otherUserId = isSender ? c.ReceiverUserId : c.SenderUserId;
            var profile = profiles.FirstOrDefault(p => p.UserId == otherUserId);
            var gender = profile?.Gender ?? "MALE";
            var placeholder = GetPlaceholder(placeholders, gender);

            return new ConnectionInvitationDto
            {
                ConnectionId = c.Id,
                OtherUserMaskedName = MaskName(profile?.DisplayName ?? "Someone"),
                OtherUserPlaceholderPhotoUrl = placeholder,
                OtherUserAgeRange = profile?.AgeRange ?? string.Empty,
                InvitationMessage = isSender ? c.SenderInvitationMessage : c.ReceiverInvitationMessage,
                InitiatedVia = c.InitiatedVia.ToString(),
                InvitedAt = c.InvitedAt,
                Status = c.InvitationStatus.ToString(),
                IsSender = isSender
            };
        }).ToList();

        return Result<List<ConnectionInvitationDto>>.Success(dtos);
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
