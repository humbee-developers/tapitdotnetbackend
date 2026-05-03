using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Location.Commands;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class LocationController : BaseApiController
{
    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));
}
