using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Account.Commands;

public record LogoutCommand(string? FcmToken) : IRequest<Result<Unit>>;

public class LogoutCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IRealTimeService realTime)
    : IRequestHandler<LogoutCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        // Deactivate the specific device token, or all devices if no token provided
        if (!string.IsNullOrWhiteSpace(cmd.FcmToken))
        {
            var device = await uow.Repository<UserDevice>().Query()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == cmd.FcmToken, ct);
            device?.Deactivate();
        }
        else
        {
            var devices = await uow.Repository<UserDevice>().Query()
                .Where(d => d.UserId == userId && d.IsActive)
                .ToListAsync(ct);
            foreach (var d in devices) d.Deactivate();
        }

        // Tap the user out so they disappear from map and system connections
        var tapStatus = await uow.Repository<Domain.Entities.TapStatus>().Query()
            .FirstOrDefaultAsync(ts => ts.UserId == userId, ct);

        if (tapStatus is null)
        {
            tapStatus = Domain.Entities.TapStatus.CreateDefault(userId);
            await uow.Repository<Domain.Entities.TapStatus>().AddAsync(tapStatus, ct);
        }

        // Use far-future date — background service won't auto-tap-in; user taps in manually after login
        tapStatus.TapOut(DateTime.UtcNow.AddYears(10), "Logged out");

        await uow.SaveChangesAsync(ct);

        await realTime.SendToUserAsync(userId, HubEvents.TapStatusChanged, new
        {
            UserId = userId,
            Status = TapStatusEnum.TapOut.ToString(),
            AutoTapInAt = (DateTime?)null,
            TapOutReason = "Logged out"
        }, ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
