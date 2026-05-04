using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Connection.Commands;
using TapitAI.Application.Features.Connection.Queries;
using TapitAI.Domain.Enums;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class ConnectionController : BaseApiController
{
    private static readonly string[] RejectionSuggestions =
    [
        "Maybe another time!",
        "Not feeling a connection, sorry.",
        "I'm not available right now.",
        "Thanks, but I'll pass.",
        "I'm looking for something different."
    ];

    private static readonly string[] PassSuggestions =
    [
        "Maybe another time!",
        "Thanks for connecting, but I'll pass.",
        "Not feeling it right now, sorry.",
        "I'm going to sit this one out.",
        "Wishing you the best!"
    ];

    private static readonly string[] ChatStarterSuggestions =
    [
        "Hey! Really glad we matched 😊",
        "Hi there! What brings you here today?",
        "Hey, I'd love to know more about you!",
        "Hi! What's your favourite thing to do around here?",
        "Hey! Looks like we've crossed paths — want to chat?",
        "Hi! I was hoping you'd say yes 😄",
        "Hey! Let's make this connection worth it ✨",
        "Hi! Coffee, tea, or just great conversation? 😄"
    ];

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(CancellationToken ct)
        => Ok(await Mediator.Send(new GetInvitationsQuery(), ct));

    [HttpGet("invitations/pending")]
    public async Task<IActionResult> GetPendingInvitation(CancellationToken ct)
        => Ok(await Mediator.Send(new GetPendingInvitationQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
        => Ok(await Mediator.Send(new GetConnectionsQuery(), ct));

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingConnection(CancellationToken ct)
        => Ok(await Mediator.Send(new GetPendingConnectionQuery(), ct));

    [HttpPost("send/{receiverUserId}")]
    public async Task<IActionResult> SendRequest(string receiverUserId, [FromBody] SendRequestBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new SendConnectionRequestCommand(
            receiverUserId, ConnectionInitiatedVia.Map, body?.Message), ct));

    [HttpPost("{connectionId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid connectionId, CancellationToken ct)
        => Ok(await Mediator.Send(new WithdrawConnectionCommand(connectionId), ct));

    [HttpGet("rejection-messages")]
    public IActionResult GetRejectionMessages()
        => Ok(RejectionSuggestions);

    [HttpGet("pass-messages")]
    public IActionResult GetPassMessages()
        => Ok(PassSuggestions);

    [HttpGet("chat-starters")]
    public IActionResult GetChatStarters()
        => Ok(ChatStarterSuggestions);

    [HttpPost("{connectionId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid connectionId, [FromBody] RejectBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new RejectConnectionCommand(connectionId, body?.Message), ct));

    [HttpPost("{connectionId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid connectionId, CancellationToken ct)
        => Ok(await Mediator.Send(new AcceptConnectionCommand(connectionId), ct));

    [HttpPost("{connectionId:guid}/pass")]
    public async Task<IActionResult> Pass(Guid connectionId, [FromBody] PassBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new PassConnectionCommand(connectionId, body?.Message), ct));

    [HttpPost("{connectionId:guid}/start-chat")]
    public async Task<IActionResult> StartChat(Guid connectionId, [FromBody] StartChatBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new StartChatCommand(connectionId, body?.Message), ct));

    public record SendRequestBody(string? Message);
    public record RejectBody(string? Message);
    public record PassBody(string? Message);
    public record StartChatBody(string? Message);
}
