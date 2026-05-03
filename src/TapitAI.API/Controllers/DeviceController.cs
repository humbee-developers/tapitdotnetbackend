using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.UserDevices.Commands;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class DeviceController : BaseApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));
}
