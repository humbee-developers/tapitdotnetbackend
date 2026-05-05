using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Connection.Commands;
using TapitAI.Application.Features.Spotlight.Commands;
using TapitAI.Application.Features.Spotlight.Queries;
using TapitAI.Domain.Enums;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class SpotlightController : BaseApiController
{
    [HttpGet]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSpotlight(CancellationToken ct)
        => Ok(await Mediator.Send(new GetCurrentSpotlightQuery(), ct));

    [HttpPost("feed/{spotlightSessionFeedId:guid}/like")]
    public async Task<IActionResult> LikeUser(Guid spotlightSessionFeedId, CancellationToken ct)
        => Ok(await Mediator.Send(new LikeSpotlightUserCommand(spotlightSessionFeedId), ct));

    [HttpDelete("feed/{spotlightSessionFeedId:guid}/like")]
    public async Task<IActionResult> UnlikeUser(Guid spotlightSessionFeedId, CancellationToken ct)
        => Ok(await Mediator.Send(new UnlikeSpotlightUserCommand(spotlightSessionFeedId), ct));

    /// <summary>Send a connection request from the spotlight feed. Same flow as map requests.</summary>
    [HttpPost("feed/{receiverUserId}/connect")]
    public async Task<IActionResult> SendConnectionRequest(
        string receiverUserId,
        [FromBody] SpotlightConnectBody? body,
        CancellationToken ct)
        => Ok(await Mediator.Send(
            new SendConnectionRequestCommand(receiverUserId, ConnectionInitiatedVia.Spotlight, body?.Message), ct));

    public record SpotlightConnectBody(string? Message);

    /// <summary>Get whether the current user appears in other users' spotlight feeds.</summary>
    [HttpGet("visibility")]
    public async Task<IActionResult> GetVisibility(CancellationToken ct)
        => Ok(await Mediator.Send(new GetSpotlightVisibilityQuery(), ct));

    /// <summary>Toggle whether the current user appears in other users' spotlight feeds.</summary>
    [HttpPost("visibility")]
    public async Task<IActionResult> SetVisibility([FromBody] SetVisibilityBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new SetSpotlightVisibilityCommand(body.IsVisible), ct));

    public record SetVisibilityBody(bool IsVisible);
}
