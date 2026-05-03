using TapitAI.Application.Common.Interfaces;

namespace TapitAI.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
