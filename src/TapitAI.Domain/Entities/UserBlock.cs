using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserBlock : AuditableEntity
{
    public string BlockerUserId { get; private set; } = null!;
    public string BlockedUserId { get; private set; } = null!;

    private UserBlock() { }

    public static UserBlock Create(string blockerUserId, string blockedUserId)
        => new() { BlockerUserId = blockerUserId, BlockedUserId = blockedUserId };
}
