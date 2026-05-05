using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Account.Commands;
using TapitAI.Application.Features.Moderation.Commands;
using TapitAI.Application.Features.Moderation.Queries;

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

    /// <summary>Get the list of users blocked by the current user.</summary>
    [HttpGet("blocked")]
    public async Task<IActionResult> GetBlocked(CancellationToken ct)
        => Ok(await Mediator.Send(new GetBlockedUsersQuery(), ct));

    /// <summary>Unblock a previously blocked user.</summary>
    [HttpDelete("blocked/{userId}")]
    public async Task<IActionResult> Unblock(string userId, CancellationToken ct)
        => Ok(await Mediator.Send(new UnblockUserCommand(userId), ct));

    public record RegisterDeviceBody(string FcmToken, string Platform);
    public record LogoutBody(string? FcmToken);
}
