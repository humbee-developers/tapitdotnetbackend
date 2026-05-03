using Microsoft.Extensions.Options;
using StreamChat.Clients;
using StreamChat.Models;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Chat;

public class StreamChatService : IChatService
{
    private readonly IStreamClientFactory _clientFactory;

    public StreamChatService(IOptions<GetStreamSettings> options)
    {
        _clientFactory = new StreamClientFactory(options.Value.ApiKey, options.Value.ApiSecret);
    }

    public async Task<ChatUserToken> CreateUserTokenAsync(
        string userId, string userName, string? imageUrl = null, CancellationToken ct = default)
    {
        await UpsertUserAsync(userId, userName, imageUrl, ct);

        var userClient = _clientFactory.GetUserClient();
        var token = userClient.CreateToken(userId);

        return new ChatUserToken(userId, token);
    }

    public async Task<ChatChannel> CreateChannelAsync(
        string channelId, string channelType, string name,
        IEnumerable<string> memberIds, CancellationToken ct = default)
    {
        var members = memberIds.ToArray();
        var channelClient = _clientFactory.GetChannelClient();

        // Simple overload: GetOrCreateAsync(channelType, channelId, creatorId, memberIds[])
        var response = await channelClient.GetOrCreateAsync(
            channelType,
            channelId,
            members.First(),
            members);

        return new ChatChannel(response.Channel.Id, response.Channel.Type, name);
    }

    public async Task AddMembersAsync(
        string channelId, string channelType, IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var channelClient = _clientFactory.GetChannelClient();
        await channelClient.AddMembersAsync(channelType, channelId, userIds.ToArray());
    }

    public async Task RemoveMembersAsync(
        string channelId, string channelType, IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var channelClient = _clientFactory.GetChannelClient();
        await channelClient.RemoveMembersAsync(channelType, channelId, userIds.ToArray());
    }

    public async Task DeleteChannelAsync(
        string channelId, string channelType, CancellationToken ct = default)
    {
        var channelClient = _clientFactory.GetChannelClient();
        await channelClient.DeleteAsync(channelType, channelId);
    }

    public async Task UpsertUserAsync(
        string userId, string userName, string? imageUrl = null, CancellationToken ct = default)
    {
        var userClient = _clientFactory.GetUserClient();
        var userRequest = new UserRequest { Id = userId, Name = userName };

        if (!string.IsNullOrEmpty(imageUrl))
            userRequest.SetData("image", imageUrl);

        await userClient.UpsertAsync(userRequest);
    }
}
