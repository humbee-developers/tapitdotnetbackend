using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using MediaEntity = TapitAI.Domain.Entities.Media;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record DeletePhotoCommand(Guid PhotoId) : IRequest<Result>;

public class DeletePhotoCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IMediaStorageService storageService)
    : IRequestHandler<DeletePhotoCommand, Result>
{
    public async Task<Result> Handle(DeletePhotoCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        var photo = profile.Photos.FirstOrDefault(ph => ph.Id == cmd.PhotoId)
            ?? throw new NotFoundException(nameof(ProfilePhoto), cmd.PhotoId);

        var media = await uow.Repository<MediaEntity>().GetByIdAsync(photo.MediaId, ct);
        if (media is not null) await storageService.DeleteAsync(media.StorageKey, ct);

        profile.RemovePhoto(photo);
        uow.Repository<ProfilePhoto>().Remove(photo);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
