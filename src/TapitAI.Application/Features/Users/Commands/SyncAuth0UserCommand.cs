using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Users;

namespace TapitAI.Application.Features.Users.Commands;

public record SyncAuth0UserCommand(
    string Auth0UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PictureUrl) : IRequest<Result<UserDto>>;

public class SyncAuth0UserCommandHandler(IIdentityService identityService)
    : IRequestHandler<SyncAuth0UserCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(SyncAuth0UserCommand request, CancellationToken ct)
        => identityService.SyncAuth0UserAsync(
            request.Auth0UserId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.PictureUrl);
}
