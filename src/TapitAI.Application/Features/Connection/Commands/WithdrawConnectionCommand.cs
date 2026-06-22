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

public record WithdrawConnectionCommand(Guid ConnectionId) : IRequest<Result<ConnectionActionResultDto>>;

public class WithdrawConnectionCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IIdentityService identity, IRealTimeService realTime)
    : IRequestHandler<WithdrawConnectionCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(WithdrawConnectionCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        if (connection.SenderUserId != currentUser.UserId)
            return Result<ConnectionActionResultDto>.Failure("Only the sender can withdraw a connection request.");

        connection.Withdraw();
        await uow.SaveChangesAsync(ct);

        var idMap = await identity.ResolveInternalUserIdsAsync(
            [connection.SenderUserId, connection.ReceiverUserId], ct);
        var senderInternalId   = idMap.GetValueOrDefault(connection.SenderUserId,   connection.SenderUserId);
        var receiverInternalId = idMap.GetValueOrDefault(connection.ReceiverUserId, connection.ReceiverUserId);

        var profiles = await ConnectionEventPayload.LoadProfilesAsync(connection, uow, ct);

        // Withdrawn event goes to the receiver — otherUser = sender (who withdrew)
        await realTime.SendToUserAsync(connection.ReceiverUserId, HubEvents.ConnectionWithdrawn,
            ConnectionEventPayload.Build(connection, senderInternalId, receiverInternalId, profiles, receiverInternalId), ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
