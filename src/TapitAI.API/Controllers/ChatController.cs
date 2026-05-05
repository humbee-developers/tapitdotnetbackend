using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Features.Chat.Queries;
using TapitAI.Application.Features.Moderation.Commands;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("active")]
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

    /// <summary>Block the other user in a chat channel. Deletes the channel and prevents future contact.</summary>
    [HttpPost("match/{channelId}/block")]
    public async Task<IActionResult> BlockUser(string channelId, CancellationToken ct)
        => Ok(await mediator.Send(new BlockUserCommand(channelId), ct));

    /// <summary>Report the other user in a chat channel.</summary>
    [HttpPost("match/{channelId}/report")]
    public async Task<IActionResult> ReportUser(
        string channelId,
        [FromBody] ReportBody body,
        CancellationToken ct)
        => Ok(await mediator.Send(new ReportUserCommand(channelId, body.Reason, body.Description, body.AlsoBlock), ct));

    public record ReportBody(string Reason, string? Description, bool AlsoBlock = false);

    /// <summary>Get the details of a specific chat channel by its channel ID.</summary>
    [HttpGet("match/{channelId}")]
    public async Task<IActionResult> GetMatchDetails(string channelId, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .FirstOrDefaultAsync(c => c.ChatChannelId == channelId, ct);

        if (connection is null)
            return NotFound(new { message = "Chat channel not found." });

        if (connection.SenderUserId != userId && connection.ReceiverUserId != userId)
            return Forbid();

        var otherUserId = connection.SenderUserId == userId
            ? connection.ReceiverUserId
            : connection.SenderUserId;

        var isBlocked = await uow.Repository<Domain.Entities.UserBlock>().Query()
            .AnyAsync(b =>
                (b.BlockerUserId == userId && b.BlockedUserId == otherUserId) ||
                (b.BlockerUserId == otherUserId && b.BlockedUserId == userId), ct);

        if (isBlocked)
            return NotFound(new { message = "Chat channel not found." });

        var otherProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == otherUserId, ct);

        var myProfile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var myDisplayName = myProfile?.DisplayName ?? userId;
        var myPhotoUrl = myProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl;
        var chatToken = await chatService.CreateUserTokenAsync(userId, myDisplayName, myPhotoUrl, ct);

        return Ok(new
        {
            ConnectionId          = connection.Id,
            ChatChannelId         = channelId,
            ApiKey                = streamSettings.Value.ApiKey,
            StreamUserId          = chatToken.UserId,
            Token                 = chatToken.Token,
            OtherUserId           = otherUserId,
            OtherUserDisplayName  = otherProfile?.DisplayName,
            OtherUserPhotoUrl     = otherProfile?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
            OtherUserAgeRange     = otherProfile?.AgeRange,
            ConnectedAt           = connection.ConnectedAt
        });
    }
}
