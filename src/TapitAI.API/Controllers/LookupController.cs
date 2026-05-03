using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Admin.Queries;
using TapitAI.Domain.Enums;

namespace TapitAI.API.Controllers;

public class LookupController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await Mediator.Send(new GetLookupOptionsQuery(null, ActiveOnly: true), ct));

    [HttpGet("{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        if (!Enum.TryParse<LookupCategory>(category, ignoreCase: true, out var cat))
            return BadRequest($"Unknown category '{category}'.");
        return Ok(await Mediator.Send(new GetLookupOptionsQuery(cat, ActiveOnly: true), ct));
    }
}
