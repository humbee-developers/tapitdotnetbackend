using TapitAI.Domain.Entities;

namespace TapitAI.Domain.Interfaces.Services;

public interface ISpotlightService
{
    Task<SpotlightSession?> GenerateForUserAsync(string userId, CancellationToken ct = default);
}
