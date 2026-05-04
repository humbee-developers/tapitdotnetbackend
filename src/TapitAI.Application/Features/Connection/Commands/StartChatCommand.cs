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

public record StartChatCommand(Guid ConnectionId, string? Message) : IRequest<Result<ConnectionActionResultDto>>;

public class StartChatCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IRealTimeService realTime,
    IFirebaseService firebase,
    IChatService chatService)
    : IRequestHandler<StartChatCommand, Result<ConnectionActionResultDto>>
{
    public async Task<Result<ConnectionActionResultDto>> Handle(StartChatCommand cmd, CancellationToken ct)
    {
        var connection = await uow.Repository<Domain.Entities.Connection>().GetByIdAsync(cmd.ConnectionId, ct)
            ?? throw new NotFoundException("Connection", cmd.ConnectionId);

        string otherUserId;
        if (connection.SenderUserId == currentUser.UserId)
        {
            connection.SenderStartChat(cmd.Message);
            otherUserId = connection.ReceiverUserId;
        }
        else if (connection.ReceiverUserId == currentUser.UserId)
        {
            connection.ReceiverStartChat(cmd.Message);
            otherUserId = connection.SenderUserId;
        }
        else
        {
            return Result<ConnectionActionResultDto>.Failure("Not a participant in this connection.");
        }

        string? chatChannelId = null;

        if (connection.IsBothConnected())
        {
            var channelId = $"connection-{connection.Id}";
            await chatService.CreateChannelAsync(
                channelId, "messaging", $"connection-{connection.Id}",
                new[] { connection.SenderUserId, connection.ReceiverUserId }, ct);

            connection.SetChatChannel(channelId);
            chatChannelId = channelId;

            var senderProfile = await uow.Repository<UserDatingProfile>().Query()
                .FirstOrDefaultAsync(p => p.UserId == connection.SenderUserId, ct);
            var receiverProfile = await uow.Repository<UserDatingProfile>().Query()
                .FirstOrDefaultAsync(p => p.UserId == connection.ReceiverUserId, ct);

            await realTime.SendToUsersAsync(
                new[] { connection.SenderUserId, connection.ReceiverUserId },
                HubEvents.ChatStarted, new
                {
                    ConnectionId = connection.Id,
                    ChatChannelId = channelId,
                    SenderConnectionMessage = connection.SenderConnectionMessage,
                    ReceiverConnectionMessage = connection.ReceiverConnectionMessage
                }, ct);

            var matchName = (currentUser.UserId == connection.SenderUserId
                ? senderProfile?.DisplayName
                : receiverProfile?.DisplayName) ?? "Your match";

            await firebase.SendToUserAsync(otherUserId,
                title: "Let's Chat!",
                body: $"{matchName} started a chat!",
                data: new Dictionary<string, string>
                {
                    ["type"]          = "ChatStarted",
                    ["connectionId"]  = connection.Id.ToString(),
                    ["chatChannelId"] = channelId
                },
                ct: ct);
        }
        else
        {
            var myProfile = await uow.Repository<UserDatingProfile>().Query()
                .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct);

            await realTime.SendToUserAsync(otherUserId, HubEvents.WaitingForPartner, new
            {
                ConnectionId = connection.Id,
                Message = cmd.Message,
                FromUserName = myProfile?.DisplayName ?? "Your match"
            }, ct);

            await firebase.SendToUserAsync(otherUserId,
                title: $"{myProfile?.DisplayName ?? "Your match"} wants to chat!",
                body: cmd.Message ?? "Tap to start the conversation.",
                data: new Dictionary<string, string>
                {
                    ["type"]         = "WaitingForPartner",
                    ["connectionId"] = connection.Id.ToString(),
                    ["message"]      = cmd.Message ?? ""
                },
                ct: ct);
        }

        await uow.SaveChangesAsync(ct);

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString(),
            ChatChannelId = chatChannelId,
            BothConnected = connection.IsBothConnected()
        });
    }
}
