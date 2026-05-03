using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Discovery.Queries;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class DiscoveryController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetNearbyUsers([FromQuery] double? radius, CancellationToken ct)
        => Ok(await Mediator.Send(new GetNearbyUsersQuery(radius), ct));
}
