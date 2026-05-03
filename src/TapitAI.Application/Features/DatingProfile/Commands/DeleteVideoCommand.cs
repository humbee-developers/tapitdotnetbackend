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

public record DeleteVideoCommand(Guid VideoId) : IRequest<Result>;

public class DeleteVideoCommandHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, IMediaStorageService storageService)
    : IRequestHandler<DeleteVideoCommand, Result>
{
    public async Task<Result> Handle(DeleteVideoCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        var video = profile.Videos.FirstOrDefault(v => v.Id == cmd.VideoId)
            ?? throw new NotFoundException(nameof(ProfileVideo), cmd.VideoId);

        var media = await uow.Repository<MediaEntity>().GetByIdAsync(video.MediaId, ct);
        if (media is not null) await storageService.DeleteAsync(media.StorageKey, ct);

        profile.RemoveVideo(video);
        uow.Repository<ProfileVideo>().Remove(video);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
