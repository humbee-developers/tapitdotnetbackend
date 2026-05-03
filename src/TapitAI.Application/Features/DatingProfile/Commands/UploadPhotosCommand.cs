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

public record UploadPhotosCommand(List<IFormFile> Photos) : IRequest<Result<List<Guid>>>;

public class UploadPhotosCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IMediaStorageService storageService)
    : IRequestHandler<UploadPhotosCommand, Result<List<Guid>>>
{
    public async Task<Result<List<Guid>>> Handle(UploadPhotosCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        var addedIds = new List<Guid>();
        var order = profile.Photos.Count;

        foreach (var file in cmd.Photos)
        {
            using var stream = file.OpenReadStream();
            var upload = await storageService.UploadAsync(new MediaUploadRequest(
                stream, file.FileName, file.ContentType,
                file.Length, Domain.Enums.MediaType.Image, "dating/photos"), ct);

            var media = MediaEntity.Create(file.FileName, file.FileName, file.ContentType,
                file.Length, Domain.Enums.MediaType.Image,
                Domain.Enums.StorageProviderType.Cloudinary,
                upload.StorageKey, upload.PublicUrl, currentUser.UserId!,
                "dating/photos");

            await uow.Repository<MediaEntity>().AddAsync(media, ct);
            await uow.SaveChangesAsync(ct);

            var photo = ProfilePhoto.Create(profile.Id, media.Id, upload.PublicUrl, order++);
            if (!profile.Photos.Any())
            {
                photo.SetAsPrimary();
                profile.SetPrimaryPhoto(photo.Id);
            }

            await uow.Repository<ProfilePhoto>().AddAsync(photo, ct);
            addedIds.Add(photo.Id);
        }

        await uow.SaveChangesAsync(ct);
        return Result<List<Guid>>.Success(addedIds);
    }
}
