using MediatR;
using TapitAI.Application.Common.Helpers;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Connection.Commands;

public record AcceptConnectionCommand(Guid ConnectionId) : IRequest<Result<ConnectionActionResultDto>>;

public class AcceptConnectionCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IIdentityService identity,
    IRealTimeService realTime, IFirebaseService firebase)
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

        var idMap = await identity.ResolveInternalUserIdsAsync(
            [connection.SenderUserId, connection.ReceiverUserId], ct);
        var senderInternalId   = idMap.GetValueOrDefault(connection.SenderUserId,   connection.SenderUserId);
        var receiverInternalId = idMap.GetValueOrDefault(connection.ReceiverUserId, connection.ReceiverUserId);

        var profiles = await ConnectionEventPayload.LoadProfilesAsync(connection, uow, ct);
        var payload  = ConnectionEventPayload.Build(connection, senderInternalId, receiverInternalId, profiles);

        await realTime.SendToUserAsync(connection.SenderUserId,   HubEvents.ConnectionAccepted, payload, ct);
        await realTime.SendToUserAsync(connection.ReceiverUserId, HubEvents.ConnectionAccepted, payload, ct);

        await firebase.SendToUserAsync(connection.SenderUserId,
            title: "Connection Accepted!",
            body: $"{profiles.ReceiverDisplayName ?? "Someone"} accepted your request!",
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionAccepted",
                ["connectionId"] = connection.Id.ToString()
            },
            ct: ct);

        await firebase.SendToUserAsync(connection.ReceiverUserId,
            title: "Connection Accepted!",
            body: $"You're now connected with {profiles.SenderDisplayName ?? "someone nearby"}!",
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionAccepted",
                ["connectionId"] = connection.Id.ToString()
            },
            ct: ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
