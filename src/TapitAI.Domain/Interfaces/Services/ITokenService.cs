namespace TapitAI.Domain.Interfaces.Services;

public record TokenResult(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");

public interface ITokenService
{
    TokenResult GenerateAdminToken(string userId, string email, IEnumerable<string> roles);
    bool ValidateToken(string token);
}
