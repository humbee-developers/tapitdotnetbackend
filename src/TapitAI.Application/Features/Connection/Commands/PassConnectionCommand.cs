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
    IUnitOfWork uow, ICurrentUserService currentUser, IRealTimeService realTime)
    : IRequestHandler<PassConnectionCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(PassConnectionCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        string otherUserId;
        if (connection.SenderUserId == currentUser.UserId)
        {
            connection.SenderPass(cmd.RejectionMessage);
            otherUserId = connection.ReceiverUserId;
        }
        else if (connection.ReceiverUserId == currentUser.UserId)
        {
            connection.ReceiverPass(cmd.RejectionMessage);
            otherUserId = connection.SenderUserId;
        }
        else
        {
            return Result<ConnectionActionResultDto>.Failure("Not a participant in this connection.");
        }

        await uow.SaveChangesAsync(ct);

        var myProfile = await uow.Repository<UserDatingProfile>().Query()
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct);

        await realTime.SendToUserAsync(otherUserId, HubEvents.ConnectionPassed, new
        {
            ConnectionId = connection.Id,
            Message = cmd.RejectionMessage ?? "The other person decided to pass.",
            FromUserName = myProfile?.DisplayName ?? "Someone"
        }, ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString()
        });
    }
}
