using MediatR;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Admin;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Admin.Commands;

public record AddLookupOptionCommand(LookupCategory Category, string Value, int SortOrder, bool IsDefault = false)
    : IRequest<Result<LookupOptionDto>>;

public record UpdateLookupOptionCommand(Guid Id, string Value, int SortOrder, bool IsActive, bool IsDefault)
    : IRequest<Result<LookupOptionDto>>;

public record DeleteLookupOptionCommand(Guid Id) : IRequest<Result>;

public class AddLookupOptionCommandHandler(IUnitOfWork uow)
    : IRequestHandler<AddLookupOptionCommand, Result<LookupOptionDto>>
{
    public async Task<Result<LookupOptionDto>> Handle(AddLookupOptionCommand cmd, CancellationToken ct)
    {
        var opt = LookupOption.Create(cmd.Category, cmd.Value, cmd.SortOrder, cmd.IsDefault);
        await uow.Repository<LookupOption>().AddAsync(opt, ct);
        await uow.SaveChangesAsync(ct);
        return Result<LookupOptionDto>.Success(LookupOptionMapper.ToDto(opt));
    }
}

public class UpdateLookupOptionCommandHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateLookupOptionCommand, Result<LookupOptionDto>>
{
    public async Task<Result<LookupOptionDto>> Handle(UpdateLookupOptionCommand cmd, CancellationToken ct)
    {
        var opt = await uow.Repository<LookupOption>().GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(LookupOption), cmd.Id);
        opt.Update(cmd.Value, cmd.SortOrder, cmd.IsActive, cmd.IsDefault);
        await uow.SaveChangesAsync(ct);
        return Result<LookupOptionDto>.Success(LookupOptionMapper.ToDto(opt));
    }
}

public class DeleteLookupOptionCommandHandler(IUnitOfWork uow)
    : IRequestHandler<DeleteLookupOptionCommand, Result>
{
    public async Task<Result> Handle(DeleteLookupOptionCommand cmd, CancellationToken ct)
    {
        var opt = await uow.Repository<LookupOption>().GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(LookupOption), cmd.Id);
        opt.Deactivate();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class LookupOptionMapper
{
    internal static LookupOptionDto ToDto(LookupOption o) => new()
    {
        Id = o.Id, Category = o.Category.ToString(), Value = o.Value,
        SortOrder = o.SortOrder, IsActive = o.IsActive, IsDefault = o.IsDefault
    };
}
