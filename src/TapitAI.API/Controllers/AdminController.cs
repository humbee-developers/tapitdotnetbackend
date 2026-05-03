using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.Admin.Commands;
using TapitAI.Application.Features.Admin.Queries;
using TapitAI.Domain.Enums;
using TapitAI.Infrastructure.Identity;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : BaseApiController
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalUsers = userManager.Users.Count();
        var adminCount = (await userManager.GetUsersInRoleAsync("Admin")).Count;
        return Ok(new { TotalUsers = totalUsers, AdminCount = adminCount, Timestamp = DateTime.UtcNow });
    }

    [HttpPost("users/{userId}/promote")]
    public async Task<IActionResult> PromoteToAdmin(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new ApplicationRole("Admin"));
        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return Ok(new { message = $"User {user.Email} promoted to Admin." });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return NoContent();
    }

    // ── Admin Settings ──────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string? category, CancellationToken ct)
        => Ok(await Mediator.Send(new GetAdminSettingsQuery(category), ct));

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateAdminSettingCommand(key, body.Value), ct));

    // ── Lookup Options ──────────────────────────────────────────────────────────

    [HttpGet("lookup")]
    public async Task<IActionResult> GetAllLookupOptions([FromQuery] LookupCategory? category, CancellationToken ct)
        => Ok(await Mediator.Send(new GetLookupOptionsQuery(category, ActiveOnly: false), ct));

    [HttpPost("lookup")]
    public async Task<IActionResult> AddLookupOption([FromBody] AddLookupOptionCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPut("lookup/{id:guid}")]
    public async Task<IActionResult> UpdateLookupOption(Guid id, [FromBody] UpdateLookupOptionBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateLookupOptionCommand(id, body.Value, body.SortOrder, body.IsActive, body.IsDefault), ct));

    [HttpDelete("lookup/{id:guid}")]
    public async Task<IActionResult> DeleteLookupOption(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new DeleteLookupOptionCommand(id), ct));

    // ── Placeholder Photos ──────────────────────────────────────────────────────

    [HttpGet("placeholder-photos")]
    public async Task<IActionResult> GetPlaceholderPhotos(CancellationToken ct)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<TapitAI.Domain.Interfaces.Repositories.IUnitOfWork>();
        var photos = await uow.Repository<TapitAI.Domain.Entities.PlaceholderPhoto>().Query()
            .OrderBy(p => p.Gender).ThenBy(p => p.DisplayOrder)
            .ToListAsyncSafe(ct);
        return Ok(photos.Select(p => new { p.Id, p.Gender, p.PhotoUrl, p.DisplayOrder, p.IsActive }));
    }

    [HttpPost("placeholder-photos")]
    public async Task<IActionResult> AddPlaceholderPhoto([FromBody] AddPlaceholderPhotoCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPut("placeholder-photos/{id:guid}")]
    public async Task<IActionResult> UpdatePlaceholderPhoto(Guid id, [FromBody] UpdatePhotoBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdatePlaceholderPhotoCommand(id, body.PhotoUrl, body.DisplayOrder, body.IsActive), ct));

    [HttpDelete("placeholder-photos/{id:guid}")]
    public async Task<IActionResult> DeletePlaceholderPhoto(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new DeletePlaceholderPhotoCommand(id), ct));

    public record UpdateSettingBody(string Value);
    public record UpdateLookupOptionBody(string Value, int SortOrder, bool IsActive, bool IsDefault);
    public record UpdatePhotoBody(string PhotoUrl, int DisplayOrder, bool IsActive);
}

internal static class QueryableExtensions
{
    internal static Task<List<T>> ToListAsyncSafe<T>(this IQueryable<T> source, CancellationToken ct)
        => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(source, ct);
}
