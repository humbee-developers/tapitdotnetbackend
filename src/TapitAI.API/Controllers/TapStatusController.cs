using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.TapStatus.Commands;
using TapitAI.Application.Features.TapStatus.Queries;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class TapStatusController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMyTapStatusQuery(), ct));

    [HttpPost("tapin")]
    public async Task<IActionResult> TapIn(CancellationToken ct)
        => Ok(await Mediator.Send(new TapInCommand(), ct));

    [HttpPost("tapout")]
    public async Task<IActionResult> TapOut([FromBody] TapOutRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new TapOutCommand(req.DurationMinutes, req.Reason), ct));

    public record TapOutRequest(int DurationMinutes, string? Reason);
}
