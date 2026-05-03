using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Auth;
using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Application.Features.Auth.Commands;

public record AdminLoginCommand(string Email, string Password) : IRequest<Result<TokenResponseDto>>;

public class AdminLoginCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService) : IRequestHandler<AdminLoginCommand, Result<TokenResponseDto>>
{
    public async Task<Result<TokenResponseDto>> Handle(AdminLoginCommand request, CancellationToken ct)
    {
        var authResult = await identityService.AuthenticateAdminAsync(request.Email, request.Password);
        if (!authResult.Succeeded)
            return Result<TokenResponseDto>.Failure(authResult.Errors);

        var user = authResult.Data!;
        var roles = await identityService.GetUserRolesAsync(user.Id);
        var token = tokenService.GenerateAdminToken(user.Id, user.Email, roles);

        return Result<TokenResponseDto>.Success(new TokenResponseDto(
            token.AccessToken,
            token.TokenType,
            token.ExpiresAt,
            user.Id,
            user.Email,
            roles));
    }
}
