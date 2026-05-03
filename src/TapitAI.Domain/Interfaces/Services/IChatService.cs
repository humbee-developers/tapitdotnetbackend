namespace TapitAI.Domain.Interfaces.Services;

public record ChatUserToken(string UserId, string Token);

public record ChatChannel(string ChannelId, string ChannelType, string Name);

public interface IChatService
{
    Task<ChatUserToken> CreateUserTokenAsync(string userId, string userName, string? imageUrl = null, CancellationToken ct = default);
    Task<ChatChannel> CreateChannelAsync(string channelId, string channelType, string name, IEnumerable<string> memberIds, CancellationToken ct = default);
    Task AddMembersAsync(string channelId, string channelType, IEnumerable<string> userIds, CancellationToken ct = default);
    Task RemoveMembersAsync(string channelId, string channelType, IEnumerable<string> userIds, CancellationToken ct = default);
    Task DeleteChannelAsync(string channelId, string channelType, CancellationToken ct = default);
    Task UpsertUserAsync(string userId, string userName, string? imageUrl = null, CancellationToken ct = default);
}
