using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Connection.Commands;

public record SendConnectionRequestCommand(
    string ReceiverUserId,
    ConnectionInitiatedVia InitiatedVia,
    string? Message = null
) : IRequest<Result<ConnectionActionResultDto>>;

public class SendConnectionRequestCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IAdminSettingService settings,
    IRealTimeService realTime,
    IFirebaseService firebase)
    : IRequestHandler<SendConnectionRequestCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(SendConnectionRequestCommand cmd, CancellationToken ct)
    {
        var senderId = currentUser.UserId!;
        var receiverId = cmd.ReceiverUserId;

        var dailySendLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsSendPerDay, 10, ct);
        var dailyReceiveLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsReceivePerDay, 5, ct);
        var connectionLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionsPerDayLimit, 3, ct);
        var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.ConnectionExpiryMinutes, 30, ct);

        var today = DateTime.UtcNow.Date;

        var sentToday = await uow.Repository<Domain.Entities.Connection>().Query()
            .CountAsync(c => c.SenderUserId == senderId && c.InvitedAt.Date == today, ct);

        if (sentToday >= dailySendLimit)
            return Result<ConnectionActionResultDto>.Failure($"Daily send limit of {dailySendLimit} connection requests reached.");

        var receivedToday = await uow.Repository<Domain.Entities.Connection>().Query()
            .CountAsync(c => c.ReceiverUserId == receiverId && c.InvitedAt.Date == today, ct);

        if (receivedToday >= dailyReceiveLimit)
            return Result<ConnectionActionResultDto>.Failure("Receiver has reached their daily incoming request limit.");

        var establishedToday = await uow.Repository<Domain.Entities.Connection>().Query()
            .CountAsync(c =>
                (c.SenderUserId == senderId || c.ReceiverUserId == senderId)
                && c.ConnectedAt.HasValue && c.ConnectedAt.Value.Date == today, ct);

        if (establishedToday >= connectionLimit)
            return Result<ConnectionActionResultDto>.Failure($"Daily connection limit of {connectionLimit} reached.");

        var senderIsActive = await uow.Repository<Domain.Entities.Connection>().Query()
            .AnyAsync(c =>
                (c.SenderUserId == senderId || c.ReceiverUserId == senderId)
                && (c.InvitationStatus == InvitationStatus.Pending
                    || c.InvitationStatus == InvitationStatus.Accepted), ct);

        if (senderIsActive)
            return Result<ConnectionActionResultDto>.Failure("You already have an active connection in progress.");

        var receiverIsActive = await uow.Repository<Domain.Entities.Connection>().Query()
            .AnyAsync(c =>
                (c.SenderUserId == receiverId || c.ReceiverUserId == receiverId)
                && (c.InvitationStatus == InvitationStatus.Pending
                    || c.InvitationStatus == InvitationStatus.Accepted), ct);

        if (receiverIsActive)
            return Result<ConnectionActionResultDto>.Failure("This person is currently in another connection.");

        var senderLocation = await uow.Repository<UserLocation>().Query()
            .FirstOrDefaultAsync(ul => ul.UserId == senderId && ul.IsLatest, ct)
            ?? throw new DomainException("Location not found. Please update your location first.");

        var receiverLocation = await uow.Repository<UserLocation>().Query()
            .FirstOrDefaultAsync(ul => ul.UserId == receiverId && ul.IsLatest, ct)
            ?? throw new DomainException("Receiver location not available.");

        var invitationMessage = cmd.Message;
        if (string.IsNullOrWhiteSpace(invitationMessage) && cmd.InitiatedVia == ConnectionInitiatedVia.System)
            invitationMessage = PickupLineProvider.GetRandom();

        var connection = Domain.Entities.Connection.Create(
            senderId, receiverId,
            senderLocation.Location.Y, senderLocation.Location.X,
            receiverLocation.Location.Y, receiverLocation.Location.X,
            cmd.InitiatedVia, invitationMessage, expiryMinutes);

        await uow.Repository<Domain.Entities.Connection>().AddAsync(connection, ct);
        await uow.SaveChangesAsync(ct);

        var senderProfile = await uow.Repository<UserDatingProfile>().Query()
            .FirstOrDefaultAsync(p => p.UserId == senderId, ct);

        var receiverProfile = await uow.Repository<UserDatingProfile>().Query()
            .FirstOrDefaultAsync(p => p.UserId == receiverId, ct);

        var placeholders = await uow.Repository<PlaceholderPhoto>().Query()
            .Where(pp => pp.IsActive).ToListAsync(ct);

        var senderMaskedName = MaskName(senderProfile?.DisplayName ?? "Someone");
        var senderGender = senderProfile?.Gender ?? "MALE";
        var senderPlaceholderPhotoUrl = GetPlaceholder(placeholders, senderGender);

        await realTime.SendToUserAsync(receiverId, HubEvents.ConnectionRequestReceived, new
        {
            ConnectionId = connection.Id,
            SenderMaskedName = senderMaskedName,
            SenderAgeRange = senderProfile?.AgeRange,
            SenderGender = senderGender,
            SenderPlaceholderPhotoUrl = senderPlaceholderPhotoUrl,
            Message = invitationMessage,
            InitiatedVia = cmd.InitiatedVia.ToString(),
            ExpiresAt = connection.ExpiresAt
        }, ct);

        await realTime.SendToUserAsync(senderId, HubEvents.ConnectionRequestSent, new
        {
            ConnectionId = connection.Id,
            ReceiverMaskedName = MaskName(receiverProfile?.DisplayName ?? "Someone"),
            ExpiresAt = connection.ExpiresAt
        }, ct);

        await firebase.SendToUserAsync(receiverId,
            title: "New Connection Request",
            body: $"{senderMaskedName} wants to connect with you!",
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionRequestReceived",
                ["connectionId"] = connection.Id.ToString(),
                ["message"]      = invitationMessage ?? ""
            },
            ct: ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }

    private static string? GetPlaceholder(List<PlaceholderPhoto> placeholders, string gender)
    {
        var match = placeholders.FirstOrDefault(p =>
            string.Equals(p.Gender, gender, StringComparison.OrdinalIgnoreCase));
        return (match ?? placeholders.FirstOrDefault())?.PhotoUrl;
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
