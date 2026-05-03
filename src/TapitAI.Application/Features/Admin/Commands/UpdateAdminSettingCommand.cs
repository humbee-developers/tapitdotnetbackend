using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Admin;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Admin.Commands;

public record UpdateAdminSettingCommand(string Key, string Value) : IRequest<Result<AdminSettingDto>>;

public class UpdateAdminSettingCommandHandler(IUnitOfWork uow, IAdminSettingService settingService)
    : IRequestHandler<UpdateAdminSettingCommand, Result<AdminSettingDto>>
{
    public async Task<Result<AdminSettingDto>> Handle(UpdateAdminSettingCommand cmd, CancellationToken ct)
    {
        var setting = await uow.Repository<AdminSetting>().Query()
            .FirstOrDefaultAsync(s => s.Key == cmd.Key, ct)
            ?? throw new NotFoundException("AdminSetting", cmd.Key);

        setting.UpdateValue(cmd.Value);
        await uow.SaveChangesAsync(ct);
        settingService.InvalidateCache(cmd.Key);

        return Result<AdminSettingDto>.Success(new AdminSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            DataType = setting.DataType,
            Description = setting.Description,
            Category = setting.Category
        });
    }
}
