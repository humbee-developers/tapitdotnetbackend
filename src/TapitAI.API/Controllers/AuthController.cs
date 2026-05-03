using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.DTOs.Auth;
using TapitAI.Application.Features.Auth.Commands;

namespace TapitAI.API.Controllers;

[AllowAnonymous]
public class AuthController : BaseApiController
{
    /// <summary>Authenticate as admin with email/password. Returns a JWT for admin endpoints.</summary>
    [HttpPost("admin/login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginDto dto, CancellationToken ct)
    {
        var result = await Mediator.Send(new AdminLoginCommand(dto.Email, dto.Password), ct);
        if (!result.Succeeded)
            return Unauthorized(new { errors = result.Errors });

        return Ok(result.Data);
    }
}
