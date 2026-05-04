using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Account.Commands;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class AccountController : BaseApiController
{
    /// <summary>Register or update the FCM push token for this device. Call on app launch after login.</summary>
    [HttpPost("device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new RegisterDeviceCommand(body.FcmToken, body.Platform), ct));

    /// <summary>Log out: deactivates the device push token and taps the user out of the map.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new LogoutCommand(body?.FcmToken), ct));

    public record RegisterDeviceBody(string FcmToken, string Platform);
    public record LogoutBody(string? FcmToken);
}
