using MediatR;
using TapitAI.Application.Common.Models;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using MediaEntity = TapitAI.Domain.Entities.Media;

namespace TapitAI.Application.Features.Media.Commands;

public record DeleteMediaCommand(Guid MediaId) : IRequest<Result>;

public class DeleteMediaCommandHandler(
    IUnitOfWork unitOfWork,
    IMediaStorageService storageService) : IRequestHandler<DeleteMediaCommand, Result>
{
    public async Task<Result> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var repo = unitOfWork.Repository<MediaEntity>();
        var media = await repo.GetByIdAsync(request.MediaId, ct)
            ?? throw new NotFoundException(nameof(MediaEntity), request.MediaId);

        await storageService.DeleteAsync(media.StorageKey, ct);
        repo.Remove(media);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
