using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Firebase;

public class FirebaseService : IFirebaseService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FirebaseService> _logger;
    private readonly FirebaseMessaging _messaging;

    public FirebaseService(AppDbContext db, IOptions<FirebaseSettings> options, ILogger<FirebaseService> logger)
    {
        _db = db;
        _logger = logger;

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(options.Value.ServiceAccountJson)
            });
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task SendToUserAsync(string userId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var tokens = await _db.Set<UserDevice>()
            .Where(d => d.UserId == userId && d.IsActive)
            .Select(d => d.FcmToken)
            .ToListAsync(ct);

        foreach (var token in tokens)
            await SendToTokenAsync(token, title, body, data, ct);
    }

    public async Task SendToTokenAsync(string token, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification { Title = title, Body = body },
                Data = data
            };
            await _messaging.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send FCM notification to token {Token}", token);
        }
    }
}
