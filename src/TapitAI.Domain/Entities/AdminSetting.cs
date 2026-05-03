using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class AdminSetting : AuditableEntity
{
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string DataType { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Category { get; private set; } = null!;

    private AdminSetting() { }

    public AdminSetting(string key, string value, string dataType, string description, string category)
    {
        Key = key;
        Value = value;
        DataType = dataType;
        Description = description;
        Category = category;
    }

    public void UpdateValue(string value) => Value = value;
}
