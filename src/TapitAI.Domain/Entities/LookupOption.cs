using TapitAI.Domain.Common;
using TapitAI.Domain.Enums;

namespace TapitAI.Domain.Entities;

public class LookupOption : AuditableEntity
{
    public LookupCategory Category { get; private set; }
    public string Value { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; }

    private LookupOption() { }

    public static LookupOption Create(LookupCategory category, string value, int sortOrder, bool isDefault = false)
        => new() { Category = category, Value = value, SortOrder = sortOrder, IsDefault = isDefault };

    public void Update(string value, int sortOrder, bool isActive, bool isDefault)
    {
        Value = value;
        SortOrder = sortOrder;
        IsActive = isActive;
        IsDefault = isDefault;
    }

    public void Deactivate() => IsActive = false;
}
