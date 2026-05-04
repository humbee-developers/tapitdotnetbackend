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

public record PassConnectionCommand(Guid ConnectionId, string? RejectionMessage) : IRequest<Result<ConnectionActionResultDto>>;

public class PassConnectionCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IRealTimeService realTime, IFirebaseService firebase)
    : IRequestHandler<PassConnectionCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(PassConnectionCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        var message = string.IsNullOrWhiteSpace(cmd.RejectionMessage)
            ? "Maybe another time!"
            : cmd.RejectionMessage;

        string otherUserId;
        if (connection.SenderUserId == currentUser.UserId)
        {
            connection.SenderPass(message);
            otherUserId = connection.ReceiverUserId;
        }
        else if (connection.ReceiverUserId == currentUser.UserId)
        {
            connection.ReceiverPass(message);
            otherUserId = connection.SenderUserId;
        }
        else
        {
            return Result<ConnectionActionResultDto>.Failure("Not a participant in this connection.");
        }

        await uow.SaveChangesAsync(ct);

        await realTime.SendToUserAsync(otherUserId, HubEvents.ConnectionPassed, new
        {
            ConnectionId = connection.Id,
            Message = message
        }, ct);

        await firebase.SendToUserAsync(otherUserId,
            title: "Your match passed",
            body: message,
            data: new Dictionary<string, string>
            {
                ["type"]         = "ConnectionPassed",
                ["connectionId"] = connection.Id.ToString(),
                ["message"]      = message
            },
            ct: ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
