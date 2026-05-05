using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Moderation.Commands;

public record BlockUserCommand(string ChannelId) : IRequest<Result>;

public class BlockUserCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser)
    : IRequestHandler<BlockUserCommand, Result>
{
    public async Task<Result> Handle(BlockUserCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        // Resolve connection from channel id
        var connection = await uow.Repository<Domain.Entities.Connection>().Query()
            .FirstOrDefaultAsync(c => c.ChatChannelId == cmd.ChannelId, ct);

        if (connection is null)
            return Result.Failure("Chat channel not found.");

        if (connection.SenderUserId != userId && connection.ReceiverUserId != userId)
            return Result.Failure("You are not a participant of this chat.");

        var otherUserId = connection.SenderUserId == userId
            ? connection.ReceiverUserId
            : connection.SenderUserId;

        // Idempotent — skip if already blocked
        var alreadyBlocked = await uow.Repository<UserBlock>().Query()
            .AnyAsync(b => b.BlockerUserId == userId && b.BlockedUserId == otherUserId, ct);

        if (!alreadyBlocked)
            await uow.Repository<UserBlock>().AddAsync(UserBlock.Create(userId, otherUserId), ct);

        connection.Block();
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
