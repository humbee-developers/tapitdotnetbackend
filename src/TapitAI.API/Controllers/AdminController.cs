using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Infrastructure.Identity;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : BaseApiController
{
    /// <summary>Get admin dashboard summary.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalUsers = userManager.Users.Count();
        var adminCount = (await userManager.GetUsersInRoleAsync("Admin")).Count;

        return Ok(new
        {
            TotalUsers = totalUsers,
            AdminCount = adminCount,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Promote a user to Admin role.</summary>
    [HttpPost("users/{userId}/promote")]
    public async Task<IActionResult> PromoteToAdmin(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new ApplicationRole("Admin"));

        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new { message = $"User {user.Email} promoted to Admin." });
    }

    /// <summary>Soft-delete a user account.</summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }
}
