using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Connection.Queries;

public record GetPendingInvitationQuery : IRequest<Result<ConnectionInvitationDto?>>;

public class GetPendingInvitationQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetPendingInvitationQuery, Result<ConnectionInvitationDto?>>
{
    public async Task<Result<ConnectionInvitationDto?>> Handle(GetPendingInvitationQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .Where(c =>
                (c.SenderUserId == userId || c.ReceiverUserId == userId)
                && c.InvitationStatus == InvitationStatus.Pending)
            .OrderByDescending(c => c.InvitedAt)
            .FirstOrDefaultAsync(ct);

        if (connection is null)
            return Result<ConnectionInvitationDto?>.Success(null);

        var isSender = connection.SenderUserId == userId;
        var otherUserId = isSender ? connection.ReceiverUserId : connection.SenderUserId;

        var profile = await uow.Repository<UserDatingProfile>().Query()
            .FirstOrDefaultAsync(p => p.UserId == otherUserId, ct);

        var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
            .Where(pp => pp.IsActive).ToListAsync(ct);

        var gender = profile?.Gender ?? "MALE";
        var placeholder = GetPlaceholder(placeholders, gender);

        return Result<ConnectionInvitationDto?>.Success(new ConnectionInvitationDto
        {
            ConnectionId = connection.Id,
            OtherUserMaskedName = MaskName(profile?.DisplayName ?? "Someone"),
            OtherUserPlaceholderPhotoUrl = placeholder,
            OtherUserAgeRange = profile?.AgeRange ?? string.Empty,
            InvitationMessage = isSender ? connection.SenderInvitationMessage : connection.ReceiverInvitationMessage,
            InitiatedVia = connection.InitiatedVia.ToString(),
            InvitedAt = connection.InvitedAt,
            ExpiresAt = connection.ExpiresAt,
            Status = connection.InvitationStatus.ToString(),
            IsSender = isSender
        });
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
