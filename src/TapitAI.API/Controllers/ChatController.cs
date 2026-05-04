using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "UserOnly")]
public class ChatController(
    IChatService chatService,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    IOptions<GetStreamSettings> streamSettings) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveChatSession(CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var displayName = profile?.DisplayName ?? userId;
        var photoUrl = profile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl;

        var chatToken = await chatService.CreateUserTokenAsync(userId, displayName, photoUrl, ct);

        return Ok(new
        {
            ApiKey = streamSettings.Value.ApiKey,
            UserId = chatToken.UserId,
            Token = chatToken.Token
        });
    }
}
