using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TapitAI.Infrastructure.Identity;

public class Auth0ClaimsTransformation(UserManager<ApplicationUser> userManager) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only enrich Auth0 tokens (they have a "sub" claim but no ASP.NET role claims)
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");

        if (sub is null || principal.HasClaim(ClaimTypes.Role, "User") || principal.HasClaim(ClaimTypes.Role, "Admin"))
            return principal;

        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Auth0UserId == sub || u.Id == sub);

        if (user is null)
            return principal;

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
            return principal;

        var identity = new ClaimsIdentity();
        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        // Also ensure NameIdentifier is the internal user Id for ICurrentUserService
        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) != user.Id)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

        principal.AddIdentity(identity);
        return principal;
    }
}
