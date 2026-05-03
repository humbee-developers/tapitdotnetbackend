using MediatR;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Admin;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Admin.Commands;

public record AddPlaceholderPhotoCommand(string Gender, string PhotoUrl, int DisplayOrder) : IRequest<Result<PlaceholderPhotoDto>>;
public record UpdatePlaceholderPhotoCommand(Guid Id, string PhotoUrl, int DisplayOrder, bool IsActive) : IRequest<Result<PlaceholderPhotoDto>>;
public record DeletePlaceholderPhotoCommand(Guid Id) : IRequest<Result>;

public class AddPlaceholderPhotoCommandHandler(IUnitOfWork uow) : IRequestHandler<AddPlaceholderPhotoCommand, Result<PlaceholderPhotoDto>>
{
    public async Task<Result<PlaceholderPhotoDto>> Handle(AddPlaceholderPhotoCommand cmd, CancellationToken ct)
    {
        var photo = PlaceholderPhoto.Create(cmd.Gender, cmd.PhotoUrl, cmd.DisplayOrder);
        await uow.Repository<PlaceholderPhoto>().AddAsync(photo, ct);
        await uow.SaveChangesAsync(ct);
        return Result<PlaceholderPhotoDto>.Success(ToDto(photo));
    }

    private static PlaceholderPhotoDto ToDto(PlaceholderPhoto p) => new()
    {
        Id = p.Id, Gender = p.Gender, PhotoUrl = p.PhotoUrl, DisplayOrder = p.DisplayOrder, IsActive = p.IsActive
    };
}

public class UpdatePlaceholderPhotoCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdatePlaceholderPhotoCommand, Result<PlaceholderPhotoDto>>
{
    public async Task<Result<PlaceholderPhotoDto>> Handle(UpdatePlaceholderPhotoCommand cmd, CancellationToken ct)
    {
        var photo = await uow.Repository<PlaceholderPhoto>().GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(PlaceholderPhoto), cmd.Id);
        photo.Update(cmd.PhotoUrl, cmd.DisplayOrder, cmd.IsActive);
        await uow.SaveChangesAsync(ct);
        return Result<PlaceholderPhotoDto>.Success(new PlaceholderPhotoDto
        {
            Id = photo.Id, Gender = photo.Gender, PhotoUrl = photo.PhotoUrl,
            DisplayOrder = photo.DisplayOrder, IsActive = photo.IsActive
        });
    }
}

public class DeletePlaceholderPhotoCommandHandler(IUnitOfWork uow) : IRequestHandler<DeletePlaceholderPhotoCommand, Result>
{
    public async Task<Result> Handle(DeletePlaceholderPhotoCommand cmd, CancellationToken ct)
    {
        var photo = await uow.Repository<PlaceholderPhoto>().GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(PlaceholderPhoto), cmd.Id);
        uow.Repository<PlaceholderPhoto>().Remove(photo);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
