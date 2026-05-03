namespace TapitAI.Application.DTOs.Auth;

public record AdminLoginDto(string Email, string Password);

public record TokenResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    IEnumerable<string> Roles);
