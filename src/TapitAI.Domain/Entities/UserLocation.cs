using NetTopologySuite.Geometries;
using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserLocation : AuditableEntity
{
    public string UserId { get; private set; } = null!;
    public Point Location { get; private set; } = null!;
    public double? Accuracy { get; private set; }
    public bool IsLatest { get; private set; } = true;

    private UserLocation() { }

    public static UserLocation Create(string userId, double longitude, double latitude, double? accuracy = null)
        => new()
        {
            UserId = userId,
            Location = new Point(longitude, latitude) { SRID = 4326 },
            Accuracy = accuracy,
            IsLatest = true
        };

    public static void MarkOldAsNonLatest(UserLocation existing) => existing.IsLatest = false;
}
