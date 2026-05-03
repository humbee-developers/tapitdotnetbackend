using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Users;

namespace TapitAI.Application.Features.Users.Queries;

public record GetUsersQuery(int PageNumber = 1, int PageSize = 20, string? SearchTerm = null)
    : IRequest<Result<PagedResult<UserDto>>>;

public class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    public Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken ct)
        => identityService.GetUsersAsync(request.PageNumber, request.PageSize, request.SearchTerm, ct);
}
