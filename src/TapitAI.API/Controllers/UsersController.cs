using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Users;
using TapitAI.Application.Features.Users.Commands;
using TapitAI.Application.Features.Users.Queries;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "AdminOrUser")]
public class UsersController : BaseApiController
{
    /// <summary>Get paginated list of users. Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetUsersQuery(pageNumber, pageSize, searchTerm), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>Sync Auth0 user profile into the system (called after Auth0 login).</summary>
    [HttpPost("sync")]
    [Authorize(Policy = "UserOnly")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncAuth0User([FromBody] SyncAuth0UserRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new SyncAuth0UserCommand(
                request.Auth0UserId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.PictureUrl), ct);

        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}

public record SyncAuth0UserRequest(
    string Auth0UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PictureUrl);
