namespace TapitAI.Application.DTOs.Admin;

public class AdminSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
}
