using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserDevice : AuditableEntity
{
    public string UserId { get; private set; } = null!;
    public string FcmToken { get; private set; } = null!;
    public string DevicePlatform { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private UserDevice() { }

    public static UserDevice Create(string userId, string fcmToken, string devicePlatform)
        => new() { UserId = userId, FcmToken = fcmToken, DevicePlatform = devicePlatform };

    public void UpdateToken(string fcmToken) => FcmToken = fcmToken;
    public void Deactivate() => IsActive = false;
    public void Reactivate(string fcmToken) { FcmToken = fcmToken; IsActive = true; }
}
