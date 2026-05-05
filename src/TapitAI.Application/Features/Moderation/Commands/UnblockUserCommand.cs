using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Moderation.Commands;

public record UnblockUserCommand(string BlockedUserId) : IRequest<Result>;

public class UnblockUserCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UnblockUserCommand, Result>
{
    public async Task<Result> Handle(UnblockUserCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var block = await uow.Repository<UserBlock>().Query()
            .FirstOrDefaultAsync(b => b.BlockerUserId == userId && b.BlockedUserId == cmd.BlockedUserId, ct);

        if (block is null)
            return Result.Failure("Block not found.");

        uow.Repository<UserBlock>().Remove(block);
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
