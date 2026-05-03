using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using MediaEntity = TapitAI.Domain.Entities.Media;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record UploadVideoCommand(IFormFile Video) : IRequest<Result<Guid>>;

public class UploadVideoCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IMediaStorageService storageService)
    : IRequestHandler<UploadVideoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadVideoCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        using var stream = cmd.Video.OpenReadStream();
        var upload = await storageService.UploadAsync(new MediaUploadRequest(
            stream, cmd.Video.FileName, cmd.Video.ContentType,
            cmd.Video.Length, Domain.Enums.MediaType.Video, "dating/videos"), ct);

        var media = MediaEntity.Create(cmd.Video.FileName, cmd.Video.FileName, cmd.Video.ContentType,
            cmd.Video.Length, Domain.Enums.MediaType.Video,
            Domain.Enums.StorageProviderType.Cloudinary,
            upload.StorageKey, upload.PublicUrl, currentUser.UserId!, "dating/videos");

        await uow.Repository<MediaEntity>().AddAsync(media, ct);
        await uow.SaveChangesAsync(ct);

        var video = ProfileVideo.Create(profile.Id, media.Id, upload.PublicUrl, profile.Videos.Count);
        profile.AddVideo(video);
        await uow.Repository<ProfileVideo>().AddAsync(video, ct);
        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(video.Id);
    }
}
