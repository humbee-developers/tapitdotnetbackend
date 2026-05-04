using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Features.Chat.Queries;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
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
    IOptions<GetStreamSettings> streamSettings,
    IMediator mediator) : ControllerBase
{
    /// <summary>Get Stream Chat credentials for initializing the mobile SDK.</summary>
    [HttpGet("token")]
    public async Task<IActionResult> GetToken(CancellationToken ct)
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
            StreamUserId = chatToken.UserId,
            Token = chatToken.Token
        });
    }

    /// <summary>Get all active chat channels (both users started chat) for the current user.</summary>
    [HttpGet("channels")]
    public async Task<IActionResult> GetChannels(CancellationToken ct)
        => Ok(await mediator.Send(new GetChatChannelsQuery(), ct));
}
