using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class ProfilePhoto : AuditableEntity
{
    public Guid UserDatingProfileId { get; private set; }
    public Guid MediaId { get; private set; }
    public string PublicUrl { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    private ProfilePhoto() { }

    public static ProfilePhoto Create(Guid profileId, Guid mediaId, string publicUrl, int displayOrder)
        => new() { UserDatingProfileId = profileId, MediaId = mediaId, PublicUrl = publicUrl, DisplayOrder = displayOrder };

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public void UpdateOrder(int order) => DisplayOrder = order;
}
