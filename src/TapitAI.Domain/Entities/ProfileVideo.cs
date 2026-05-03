using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class ProfileVideo : AuditableEntity
{
    public Guid UserDatingProfileId { get; private set; }
    public Guid MediaId { get; private set; }
    public string PublicUrl { get; private set; } = null!;
    public int DisplayOrder { get; private set; }

    private ProfileVideo() { }

    public static ProfileVideo Create(Guid profileId, Guid mediaId, string publicUrl, int displayOrder)
        => new() { UserDatingProfileId = profileId, MediaId = mediaId, PublicUrl = publicUrl, DisplayOrder = displayOrder };

    public void UpdateOrder(int order) => DisplayOrder = order;
}
