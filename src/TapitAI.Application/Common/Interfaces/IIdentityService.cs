using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Users;

namespace TapitAI.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<UserDto>> AuthenticateAdminAsync(string email, string password);
    Task<Result<UserDto>> SyncAuth0UserAsync(string auth0UserId, string email, string? firstName, string? lastName, string? pictureUrl);
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Resolves an Auth0 sub (e.g. "auth0|abc123") or an already-internal UUID to the
    /// internal ApplicationUser.Id. Returns null if no matching user is found.
    /// </summary>
    Task<string?> ResolveInternalUserIdAsync(string auth0SubOrId, CancellationToken ct = default);

    /// <summary>
    /// Batch-resolves a collection of Auth0 subs / internal UUIDs to internal UUIDs.
    /// Returns a dictionary keyed by the original value; unmapped entries are omitted.
    /// </summary>
    Task<Dictionary<string, string>> ResolveInternalUserIdsAsync(IEnumerable<string> auth0SubsOrIds, CancellationToken ct = default);
}
