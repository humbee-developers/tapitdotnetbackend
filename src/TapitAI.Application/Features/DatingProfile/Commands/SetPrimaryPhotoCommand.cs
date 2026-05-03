using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record SetPrimaryPhotoCommand(Guid PhotoId) : IRequest<Result>;

public class SetPrimaryPhotoCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<SetPrimaryPhotoCommand, Result>
{
    public async Task<Result> Handle(SetPrimaryPhotoCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        var target = profile.Photos.FirstOrDefault(ph => ph.Id == cmd.PhotoId)
            ?? throw new NotFoundException(nameof(ProfilePhoto), cmd.PhotoId);

        foreach (var ph in profile.Photos) ph.UnsetPrimary();
        target.SetAsPrimary();
        profile.SetPrimaryPhoto(target.Id);

        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
