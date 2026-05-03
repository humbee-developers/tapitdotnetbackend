using Microsoft.AspNetCore.Identity;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Users;

namespace TapitAI.Infrastructure.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    public async Task<Result<UserDto>> AuthenticateAdminAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.IsDeleted)
            return Result<UserDto>.Failure("Invalid email or password.");

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<UserDto>.Failure(result.IsLockedOut ? "Account is locked out." : "Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains("Admin"))
            return Result<UserDto>.Failure("Access denied.");

        return Result<UserDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result<UserDto>> SyncAuth0UserAsync(
        string auth0UserId, string email, string? firstName, string? lastName, string? pictureUrl)
    {
        var existing = await userManager.FindByLoginAsync("Auth0", auth0UserId);
        if (existing is not null)
        {
            existing.FirstName = firstName ?? existing.FirstName;
            existing.LastName = lastName ?? existing.LastName;
            existing.ProfileImageUrl = pictureUrl ?? existing.ProfileImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(existing);

            var existingRoles = await userManager.GetRolesAsync(existing);
            return Result<UserDto>.Success(MapToDto(existing, existingRoles));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            ProfileImageUrl = pictureUrl,
            Auth0UserId = auth0UserId
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return Result<UserDto>.Failure(createResult.Errors.Select(e => e.Description));

        await userManager.AddLoginAsync(user, new UserLoginInfo("Auth0", auth0UserId, "Auth0"));
        await userManager.AddToRoleAsync(user, "User");

        return Result<UserDto>.Success(MapToDto(user, ["User"]));
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        int pageNumber, int pageSize, string? searchTerm, CancellationToken ct = default)
    {
        var query = userManager.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(searchTerm)) ||
                (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                (u.LastName != null && u.LastName.Contains(searchTerm)));

        var total = query.Count();
        var users = query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<UserDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            dtos.Add(MapToDto(u, roles));
        }

        return Result<PagedResult<UserDto>>.Success(
            PagedResult<UserDto>.Create(dtos, total, pageNumber, pageSize));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted) return null;

        var roles = await userManager.GetRolesAsync(user);
        return MapToDto(user, roles);
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return [];
        return await userManager.GetRolesAsync(user);
    }

    private static UserDto MapToDto(ApplicationUser user, IEnumerable<string> roles)
        => new(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.ProfileImageUrl,
            string.Join(",", roles),
            user.IsActive,
            user.CreatedAt);
}
