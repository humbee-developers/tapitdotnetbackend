namespace TapitAI.Application.DTOs.Admin;

public class LookupOptionDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}
