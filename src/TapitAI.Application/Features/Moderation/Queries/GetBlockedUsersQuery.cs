using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Moderation.Queries;

public record GetBlockedUsersQuery : IRequest<Result<List<BlockedUserDto>>>;

public record BlockedUserDto(string UserId, string? DisplayName, DateTime BlockedAt);

public class GetBlockedUsersQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetBlockedUsersQuery, Result<List<BlockedUserDto>>>
{
    public async Task<Result<List<BlockedUserDto>>> Handle(GetBlockedUsersQuery _, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var blocks = await uow.Repository<UserBlock>().Query()
            .AsNoTracking()
            .Where(b => b.BlockerUserId == userId)
            .ToListAsync(ct);

        var blockedIds = blocks.Select(b => b.BlockedUserId).ToList();

        var profiles = await uow.Repository<UserDatingProfile>().Query()
            .AsNoTracking()
            .Where(p => blockedIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var result = blocks
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BlockedUserDto(
                b.BlockedUserId,
                profiles.TryGetValue(b.BlockedUserId, out var p) ? p.DisplayName : null,
                b.CreatedAt))
            .ToList();

        return Result<List<BlockedUserDto>>.Success(result);
    }
}
