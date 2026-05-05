using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserReport : AuditableEntity
{
    public string ReporterUserId { get; private set; } = null!;
    public string ReportedUserId { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? ConnectionId { get; private set; }

    private UserReport() { }

    public static UserReport Create(
        string reporterUserId, string reportedUserId,
        string reason, string? description, Guid? connectionId)
        => new()
        {
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            Reason = reason,
            Description = description,
            ConnectionId = connectionId
        };
}
