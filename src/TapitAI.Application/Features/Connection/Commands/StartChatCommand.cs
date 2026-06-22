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

public record StartChatCommand(Guid ConnectionId, string? Message) : IRequest<Result<ConnectionActionResultDto>>;

public class StartChatCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IIdentityService identity,
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

        var idMap = await identity.ResolveInternalUserIdsAsync(
            [connection.SenderUserId, connection.ReceiverUserId], ct);
        var senderInternalId   = idMap.GetValueOrDefault(connection.SenderUserId,   connection.SenderUserId);
        var receiverInternalId = idMap.GetValueOrDefault(connection.ReceiverUserId, connection.ReceiverUserId);

        var profiles = await ConnectionEventPayload.LoadProfilesAsync(connection, uow, ct);

        if (connection.IsBothConnected())
        {
            var channelId = $"connection-{connection.Id}";

            // Create the GetStream channel first — fully awaited before we touch anything else.
            await chatService.CreateChannelAsync(
                channelId, "messaging", $"connection-{connection.Id}",
                new[] { connection.SenderUserId, connection.ReceiverUserId }, ct);

            connection.SetChatChannel(channelId);
            chatChannelId = channelId;

            // Persist to DB before firing events so the channel ID is durable
            // and GetStream's write has fully landed before the client opens it.
            await uow.SaveChangesAsync(ct);

            var historicalMessages = new[]
            {
                (connection.SenderUserId,   connection.SenderInvitationMessage),
                (connection.SenderUserId,   connection.SenderConnectionMessage),
                (connection.ReceiverUserId, connection.ReceiverConnectionMessage),
            };

            foreach (var (msgUserId, text) in historicalMessages)
                if (!string.IsNullOrWhiteSpace(text))
                    await chatService.SendMessageAsync(channelId, "messaging", msgUserId, text, ct);

            // Guard: never fire ChatStarted without a channel ID.
            if (string.IsNullOrEmpty(connection.ChatChannelId))
                throw new InvalidOperationException("ChatChannelId must be set before firing ChatStarted.");

            await realTime.SendToUserAsync(connection.SenderUserId, HubEvents.ChatStarted,
                ConnectionEventPayload.Build(connection, senderInternalId, receiverInternalId, profiles, senderInternalId), ct);

            await realTime.SendToUserAsync(connection.ReceiverUserId, HubEvents.ChatStarted,
                ConnectionEventPayload.Build(connection, senderInternalId, receiverInternalId, profiles, receiverInternalId), ct);

            var matchName = (currentUser.UserId == connection.SenderUserId
                ? profiles.SenderDisplayName
                : profiles.ReceiverDisplayName) ?? "Your match";

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
            // Persist the partial connect status so the other user's call sees it.
            await uow.SaveChangesAsync(ct);

            var waitingViewerId = otherUserId == connection.SenderUserId ? senderInternalId : receiverInternalId;
            await realTime.SendToUserAsync(otherUserId, HubEvents.WaitingForPartner,
                ConnectionEventPayload.Build(connection, senderInternalId, receiverInternalId, profiles, waitingViewerId), ct);

            var myDisplayName = (currentUser.UserId == connection.SenderUserId
                ? profiles.SenderDisplayName
                : profiles.ReceiverDisplayName) ?? "Your match";

            await firebase.SendToUserAsync(otherUserId,
                title: $"{myDisplayName} wants to chat!",
                body: cmd.Message ?? "Tap to start the conversation.",
                data: new Dictionary<string, string>
                {
                    ["type"]         = "WaitingForPartner",
                    ["connectionId"] = connection.Id.ToString(),
                    ["message"]      = cmd.Message ?? ""
                },
                ct: ct);
        }

        return Result<ConnectionActionResultDto>.Success(new ConnectionActionResultDto
        {
            ConnectionId = connection.Id,
            Status = connection.InvitationStatus.ToString(),
            ChatChannelId = chatChannelId,
            BothConnected = connection.IsBothConnected()
        });
    }
}
