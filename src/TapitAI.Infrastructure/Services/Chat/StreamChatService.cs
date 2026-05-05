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

    // Stream Chat only allows: a-z, 0-9, @, _, ., space, -
    // Auth0 IDs contain '|' which is invalid — replace with '-'
    private static string ToStreamId(string userId) => userId.Replace('|', '-');

    public async Task<ChatUserToken> CreateUserTokenAsync(
        string userId, string userName, string? imageUrl = null, CancellationToken ct = default)
    {
        var streamId = ToStreamId(userId);
        await UpsertUserAsync(userId, userName, imageUrl, ct);

        var userClient = _clientFactory.GetUserClient();
        var token = userClient.CreateToken(streamId);

        return new ChatUserToken(streamId, token);
    }

    public async Task<ChatChannel> CreateChannelAsync(
        string channelId, string channelType, string name,
        IEnumerable<string> memberIds, CancellationToken ct = default)
    {
        var members = memberIds.Select(ToStreamId).ToArray();
        var channelClient = _clientFactory.GetChannelClient();

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
        await channelClient.AddMembersAsync(channelType, channelId, userIds.Select(ToStreamId).ToArray());
    }

    public async Task RemoveMembersAsync(
        string channelId, string channelType, IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var channelClient = _clientFactory.GetChannelClient();
        await channelClient.RemoveMembersAsync(channelType, channelId, userIds.Select(ToStreamId).ToArray());
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
        var streamId = ToStreamId(userId);
        var userClient = _clientFactory.GetUserClient();
        var userRequest = new UserRequest { Id = streamId, Name = userName };

        if (!string.IsNullOrEmpty(imageUrl))
            userRequest.SetData("image", imageUrl);

        await userClient.UpsertAsync(userRequest);
    }

    public async Task SendMessageAsync(
        string channelId, string channelType, string senderId, string text, CancellationToken ct = default)
    {
        var streamSenderId = ToStreamId(senderId);
        var messageClient = _clientFactory.GetMessageClient();
        await messageClient.SendMessageAsync(channelType, channelId, streamSenderId, text);
    }
}
