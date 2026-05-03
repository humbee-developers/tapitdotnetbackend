using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class PlaceholderPhoto : AuditableEntity
{
    public string Gender { get; private set; } = null!;
    public string PhotoUrl { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private PlaceholderPhoto() { }

    public static PlaceholderPhoto Create(string gender, string photoUrl, int displayOrder)
        => new() { Gender = gender, PhotoUrl = photoUrl, DisplayOrder = displayOrder };

    public void Update(string photoUrl, int displayOrder, bool isActive)
    {
        PhotoUrl = photoUrl;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}
