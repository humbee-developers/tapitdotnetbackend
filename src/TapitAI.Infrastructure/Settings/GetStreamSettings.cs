namespace TapitAI.Infrastructure.Settings;

public class GetStreamSettings
{
    public const string SectionName = "GetStream";
    public string ApiKey { get; init; } = null!;
    public string ApiSecret { get; init; } = null!;
}
