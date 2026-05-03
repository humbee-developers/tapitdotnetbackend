using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Admin;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Admin.Queries;

public record GetAdminSettingsQuery(string? Category = null) : IRequest<Result<List<AdminSettingDto>>>;

public class GetAdminSettingsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetAdminSettingsQuery, Result<List<AdminSettingDto>>>
{
    public async Task<Result<List<AdminSettingDto>>> Handle(GetAdminSettingsQuery qry, CancellationToken ct)
    {
        var query = uow.Repository<AdminSetting>().Query();
        if (!string.IsNullOrWhiteSpace(qry.Category))
            query = query.Where(s => s.Category == qry.Category);

        var settings = await query.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync(ct);

        return Result<List<AdminSettingDto>>.Success(settings.Select(s => new AdminSettingDto
        {
            Id = s.Id, Key = s.Key, Value = s.Value,
            DataType = s.DataType, Description = s.Description, Category = s.Category
        }).ToList());
    }
}
