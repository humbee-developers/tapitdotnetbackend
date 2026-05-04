using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Account.Commands;

public record RegisterDeviceCommand(string FcmToken, string Platform) : IRequest<Result<Unit>>;

public class RegisterDeviceCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser)
    : IRequestHandler<RegisterDeviceCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RegisterDeviceCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;
        var platform = cmd.Platform.ToLowerInvariant();

        var existing = await uow.Repository<UserDevice>().Query()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DevicePlatform == platform, ct);

        if (existing is not null)
            existing.Reactivate(cmd.FcmToken);
        else
            await uow.Repository<UserDevice>().AddAsync(
                UserDevice.Create(userId, cmd.FcmToken, platform), ct);

        await uow.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
