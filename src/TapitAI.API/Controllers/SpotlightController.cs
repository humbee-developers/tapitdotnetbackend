using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Spotlight.Commands;
using TapitAI.Application.Features.Spotlight.Queries;

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
}
