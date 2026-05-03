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
}
