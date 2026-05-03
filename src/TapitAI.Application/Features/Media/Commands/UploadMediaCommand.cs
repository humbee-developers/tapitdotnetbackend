using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Media;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using MediaEntity = TapitAI.Domain.Entities.Media;

namespace TapitAI.Application.Features.Media.Commands;

public record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Domain.Enums.MediaType MediaType,
    string? Folder = null) : IRequest<Result<MediaDto>>;

public class UploadMediaCommandHandler(
    IMediaStorageService storageService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<UploadMediaCommand, Result<MediaDto>>
{
    public async Task<Result<MediaDto>> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var uploadRequest = new MediaUploadRequest(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.MediaType,
            request.Folder);

        var uploadResult = await storageService.UploadAsync(uploadRequest, ct);

        var media = MediaEntity.Create(
            request.FileName,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.MediaType,
            uploadResult.Provider,
            uploadResult.StorageKey,
            uploadResult.PublicUrl,
            currentUser.UserId!,
            request.Folder);

        var repo = unitOfWork.Repository<MediaEntity>();
        await repo.AddAsync(media, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<MediaDto>.Success(new MediaDto(
            media.Id,
            media.FileName,
            media.OriginalFileName,
            media.ContentType,
            media.FileSizeBytes,
            media.MediaType,
            media.PublicUrl,
            media.Folder,
            media.CreatedAt));
    }
}
