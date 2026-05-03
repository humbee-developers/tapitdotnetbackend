using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Connection.Commands;

public record RejectConnectionCommand(Guid ConnectionId, string? RejectionMessage) : IRequest<Result<ConnectionActionResultDto>>;

public class RejectConnectionCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IRealTimeService realTime, IFirebaseService firebase)
    : IRequestHandler<RejectConnectionCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(RejectConnectionCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        if (connection.ReceiverUserId != currentUser.UserId)
            return Result<ConnectionActionResultDto>.Failure("Only the receiver can reject a connection request.");

        connection.Reject(cmd.RejectionMessage);
        await uow.SaveChangesAsync(ct);

        await realTime.SendToUserAsync(connection.SenderUserId, HubEvents.ConnectionRejected, new
        {
            ConnectionId = connection.Id,
            RejectionMessage = cmd.RejectionMessage,
            Message = "Your connection request was declined."
        }, ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
