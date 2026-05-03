namespace TapitAI.Application.DTOs.Dating;

public class TapStatusDto
{
    public string UserId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? AutoTapInAt { get; set; }
    public string? TapOutReason { get; set; }
}
