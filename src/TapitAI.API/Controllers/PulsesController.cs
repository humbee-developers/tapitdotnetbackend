using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Pulses.Queries;

namespace TapitAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "UserOnly")]
public class PulsesController : BaseApiController
{
    [HttpGet("sent")]
    public async Task<IActionResult> GetSentPulses(CancellationToken ct)
        => Ok(await Mediator.Send(new GetSentPulsesQuery(), ct));
}
