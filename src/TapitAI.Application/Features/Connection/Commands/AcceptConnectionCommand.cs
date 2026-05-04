using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
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

        var revealPayloadForSender = new
        {
            ConnectionId = connection.Id,
            OtherUserDisplayName = receiverProfile?.DisplayName,
            OtherUserPhotoUrl = receiverProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange = receiverProfile?.AgeRange,
            Message = "Your connection request was accepted!"
        };

        var revealPayloadForReceiver = new
        {
            ConnectionId = connection.Id,
            OtherUserDisplayName = senderProfile?.DisplayName,
            OtherUserPhotoUrl = senderProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange = senderProfile?.AgeRange,
            Message = "You accepted the connection request!"
        };

        await realTime.SendToUserAsync(connection.SenderUserId, HubEvents.ConnectionAccepted, revealPayloadForSender, ct);
        await realTime.SendToUserAsync(connection.ReceiverUserId, HubEvents.ConnectionAccepted, revealPayloadForReceiver, ct);

        var senderPhotoUrl = senderProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl ?? "";
        var receiverPhotoUrl = receiverProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl ?? "";

        await firebase.SendToUserAsync(connection.SenderUserId,
            title: "Connection Accepted!",
            body: $"{receiverProfile?.DisplayName ?? "Someone"} accepted your request!",
            data: new Dictionary<string, string>
            {
                ["type"]              = "ConnectionAccepted",
                ["connectionId"]      = connection.Id.ToString(),
                ["otherUserName"]     = receiverProfile?.DisplayName ?? "",
                ["otherUserPhotoUrl"] = receiverPhotoUrl,
                ["otherUserAgeRange"] = receiverProfile?.AgeRange ?? "",
                ["message"]           = "Your connection request was accepted!"
            },
            ct: ct);

        await firebase.SendToUserAsync(connection.ReceiverUserId,
            title: "Connection Revealed!",
            body: $"You're now connected with {senderProfile?.DisplayName ?? "someone nearby"}!",
            data: new Dictionary<string, string>
            {
                ["type"]              = "ConnectionAccepted",
                ["connectionId"]      = connection.Id.ToString(),
                ["otherUserName"]     = senderProfile?.DisplayName ?? "",
                ["otherUserPhotoUrl"] = senderPhotoUrl,
                ["otherUserAgeRange"] = senderProfile?.AgeRange ?? "",
                ["message"]           = "You accepted the connection request!"
            },
            ct: ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
