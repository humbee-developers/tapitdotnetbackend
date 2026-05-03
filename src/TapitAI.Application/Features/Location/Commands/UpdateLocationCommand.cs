using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Location.Commands;

public record UpdateLocationCommand(double Longitude, double Latitude, double? Accuracy) : IRequest<Result>;

public class UpdateLocationCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateLocationCommand, Result>
{
    public async Task<Result> Handle(UpdateLocationCommand cmd, CancellationToken ct)
    {
        var existing = await uow.Repository<UserLocation>().Query()
            .Where(ul => ul.UserId == currentUser.UserId && ul.IsLatest)
            .ToListAsync(ct);

        foreach (var old in existing) UserLocation.MarkOldAsNonLatest(old);

        var location = UserLocation.Create(currentUser.UserId!, cmd.Longitude, cmd.Latitude, cmd.Accuracy);
        await uow.Repository<UserLocation>().AddAsync(location, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
