using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Connection.Commands;
using TapitAI.Application.Features.Connection.Queries;
using TapitAI.Domain.Enums;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class ConnectionController : BaseApiController
{
    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(CancellationToken ct)
        => Ok(await Mediator.Send(new GetInvitationsQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
        => Ok(await Mediator.Send(new GetConnectionsQuery(), ct));

    [HttpPost("send/{receiverUserId}")]
    public async Task<IActionResult> SendRequest(string receiverUserId, [FromBody] SendRequestBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new SendConnectionRequestCommand(
            receiverUserId, ConnectionInitiatedVia.Map, body?.Message), ct));

    [HttpPost("{connectionId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid connectionId, CancellationToken ct)
        => Ok(await Mediator.Send(new WithdrawConnectionCommand(connectionId), ct));

    [HttpPost("{connectionId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid connectionId, [FromBody] RejectBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new RejectConnectionCommand(connectionId, body.Message), ct));

    [HttpPost("{connectionId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid connectionId, CancellationToken ct)
        => Ok(await Mediator.Send(new AcceptConnectionCommand(connectionId), ct));

    [HttpPost("{connectionId:guid}/pass")]
    public async Task<IActionResult> Pass(Guid connectionId, [FromBody] PassBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new PassConnectionCommand(connectionId, body.Message), ct));

    [HttpPost("{connectionId:guid}/start-chat")]
    public async Task<IActionResult> StartChat(Guid connectionId, [FromBody] StartChatBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new StartChatCommand(connectionId, body?.Message), ct));

    public record SendRequestBody(string? Message);
    public record RejectBody(string? Message);
    public record PassBody(string? Message);
    public record StartChatBody(string? Message);
}
