using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Connection.Commands;

public record AcceptConnectionCommand(Guid ConnectionId) : IRequest<Result<ConnectionActionResultDto>>;

public class AcceptConnectionCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IRealTimeService realTime, IFirebaseService firebase)
    : IRequestHandler<AcceptConnectionCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(AcceptConnectionCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        if (connection.ReceiverUserId != currentUser.UserId)
            return Result<ConnectionActionResultDto>.Failure("Only the receiver can accept a connection request.");

        connection.Accept();
        await uow.SaveChangesAsync(ct);

        var senderProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == connection.SenderUserId, ct);

        var receiverProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == connection.ReceiverUserId, ct);

        // System connections keep identity hidden until both participants connect.
        var isSystemConnection = connection.InitiatedVia == ConnectionInitiatedVia.System;

        var payloadForSender = new
        {
            ConnectionId = connection.Id,
            IsIdentityHidden = isSystemConnection,
            OtherUserDisplayName = isSystemConnection ? MaskName(receiverProfile?.DisplayName ?? "Someone") : receiverProfile?.DisplayName,
            OtherUserPhotoUrl = isSystemConnection ? (string?)null : receiverProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange = receiverProfile?.AgeRange,
            Message = "Your connection request was accepted!"
        };

        var payloadForReceiver = new
        {
            ConnectionId = connection.Id,
            IsIdentityHidden = isSystemConnection,
            OtherUserDisplayName = isSystemConnection ? MaskName(senderProfile?.DisplayName ?? "Someone") : senderProfile?.DisplayName,
            OtherUserPhotoUrl = isSystemConnection ? (string?)null : senderProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange = senderProfile?.AgeRange,
            Message = "You accepted the connection request!"
        };

        await realTime.SendToUserAsync(connection.SenderUserId, HubEvents.ConnectionAccepted, payloadForSender, ct);
        await realTime.SendToUserAsync(connection.ReceiverUserId, HubEvents.ConnectionAccepted, payloadForReceiver, ct);

        await firebase.SendToUserAsync(connection.SenderUserId,
            title: "Connection Accepted!",
            body: isSystemConnection ? "Someone nearby accepted your request!" : $"{receiverProfile?.DisplayName ?? "Someone"} accepted your request!",
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionAccepted",
                ["connectionId"] = connection.Id.ToString(),
                ["message"]      = "Your connection request was accepted!"
            },
            ct: ct);

        await firebase.SendToUserAsync(connection.ReceiverUserId,
            title: "Connection Accepted!",
            body: "You accepted — now decide if you want to connect!",
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionAccepted",
                ["connectionId"] = connection.Id.ToString(),
                ["message"]      = "You accepted the connection request!"
            },
            ct: ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
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
