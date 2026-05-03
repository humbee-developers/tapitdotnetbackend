using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Admin;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.Admin.Queries;

public record GetLookupOptionsQuery(LookupCategory? Category = null, bool ActiveOnly = true)
    : IRequest<Result<List<LookupOptionDto>>>;

public class GetLookupOptionsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetLookupOptionsQuery, Result<List<LookupOptionDto>>>
{
    public async Task<Result<List<LookupOptionDto>>> Handle(GetLookupOptionsQuery qry, CancellationToken ct)
    {
        var query = uow.Repository<LookupOption>().Query();
        if (qry.Category.HasValue) query = query.Where(o => o.Category == qry.Category.Value);
        if (qry.ActiveOnly) query = query.Where(o => o.IsActive);

        var opts = await query.OrderBy(o => o.Category).ThenBy(o => o.SortOrder).ToListAsync(ct);

        return Result<List<LookupOptionDto>>.Success(opts.Select(o => new LookupOptionDto
        {
            Id = o.Id, Category = o.Category.ToString(), Value = o.Value,
            SortOrder = o.SortOrder, IsActive = o.IsActive, IsDefault = o.IsDefault
        }).ToList());
    }
}
