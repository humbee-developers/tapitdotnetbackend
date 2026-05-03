using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.UserDevices.Commands;

public record RegisterDeviceCommand(string FcmToken, string DevicePlatform) : IRequest<Result>;

public class RegisterDeviceCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<RegisterDeviceCommand, Result>
{
    public async Task<Result> Handle(RegisterDeviceCommand cmd, CancellationToken ct)
    {
        var existing = await uow.Repository<UserDevice>().Query()
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId && d.DevicePlatform == cmd.DevicePlatform, ct);

        if (existing is not null)
        {
            existing.UpdateToken(cmd.FcmToken);
        }
        else
        {
            var device = UserDevice.Create(currentUser.UserId!, cmd.FcmToken, cmd.DevicePlatform);
            await uow.Repository<UserDevice>().AddAsync(device, ct);
        }

        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
