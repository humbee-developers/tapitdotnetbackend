namespace TapitAI.Infrastructure.Settings;

public class RedisSettings
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; init; } = null!;
    public int DefaultExpiryMinutes { get; init; } = 60;
}
