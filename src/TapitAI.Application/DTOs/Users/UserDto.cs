namespace TapitAI.Application.DTOs.Users;

public record UserDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public record CreateUserDto(
    string Email,
    string? FirstName,
    string? LastName);

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl);
