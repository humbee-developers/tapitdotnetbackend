using TapitAI.Domain.Common;
using TapitAI.Domain.Enums;

namespace TapitAI.Domain.Entities;

public class TapStatus : AuditableEntity
{
    public string UserId { get; private set; } = null!;
    public TapStatusEnum Status { get; private set; } = TapStatusEnum.TapIn;
    public DateTime? AutoTapInAt { get; private set; }
    public string? TapOutReason { get; private set; }

    private TapStatus() { }

    public static TapStatus CreateDefault(string userId)
        => new() { UserId = userId, Status = TapStatusEnum.TapIn };

    public void TapIn()
    {
        Status = TapStatusEnum.TapIn;
        AutoTapInAt = null;
        TapOutReason = null;
    }

    public void TapOut(DateTime autoTapInAt, string? reason = null)
    {
        Status = TapStatusEnum.TapOut;
        AutoTapInAt = autoTapInAt;
        TapOutReason = reason;
    }
}
