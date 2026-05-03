namespace TapitAI.Infrastructure.Settings;

public class Auth0Settings
{
    public const string SectionName = "Auth0";
    public string Domain { get; init; } = null!;
    public string Audience { get; init; } = null!;
}
