using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Data.Seeds;

public static class DatingAppSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedAdminSettingsAsync(db);
        await SeedPlaceholderPhotosAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminSettingsAsync(AppDbContext db)
    {
        var defaults = new[]
        {
            (AdminSettingKeys.DiscoveryRadiusMiles, "100", "double", "Discovery radius shown on map (miles)", "Discovery"),
            (AdminSettingKeys.ConnectionsPerDayLimit, "3", "int", "Max established connections per user per day", "Connection"),
            (AdminSettingKeys.ConnectionRequestsSendPerDay, "10", "int", "Max connection requests a user can send per day", "Connection"),
            (AdminSettingKeys.ConnectionRequestsReceivePerDay, "5", "int", "Max connection requests a user can receive per day", "Connection"),
            (AdminSettingKeys.ConnectionExpiryMinutes, "30", "int", "Minutes before a pending connection request expires", "Connection"),
            (AdminSettingKeys.ConnectionRadiusMiles, "25", "double", "Radius for system-initiated connections (miles)", "Connection"),
            (AdminSettingKeys.SpotlightGenerationIntervalMinutes, "60", "int", "How often spotlight sessions are regenerated (minutes)", "Spotlight"),
            (AdminSettingKeys.SpotlightRadiusMiles, "100", "double", "Radius for spotlight user discovery (miles)", "Spotlight"),
            (AdminSettingKeys.SpotlightMaxUsers, "5", "int", "Max users shown in a spotlight session", "Spotlight"),
            (AdminSettingKeys.SpotlightExpiryMinutes, "60", "int", "Minutes a spotlight session remains active", "Spotlight"),
            (AdminSettingKeys.SystemConnectionIntervalSeconds, "30", "int", "How often the system tries to create automatic connections (seconds)", "System"),
        };

        foreach (var (key, value, dataType, description, category) in defaults)
        {
            if (!db.Set<AdminSetting>().Any(s => s.Key == key))
                db.Set<AdminSetting>().Add(new AdminSetting(key, value, dataType, description, category));
        }
    }

    private static async Task SeedPlaceholderPhotosAsync(AppDbContext db)
    {
        if (db.Set<PlaceholderPhoto>().Any()) return;

        var photos = new List<PlaceholderPhoto>();

        for (var i = 1; i <= 8; i++)
            photos.Add(PlaceholderPhoto.Create("MALE", $"https://randomuser.me/api/portraits/men/{i + 10}.jpg", i));

        for (var i = 1; i <= 8; i++)
            photos.Add(PlaceholderPhoto.Create("FEMALE", $"https://randomuser.me/api/portraits/women/{i + 10}.jpg", i));

        for (var i = 1; i <= 5; i++)
            photos.Add(PlaceholderPhoto.Create("NON_BINARY", $"https://randomuser.me/api/portraits/lego/{i}.jpg", i));

        db.Set<PlaceholderPhoto>().AddRange(photos);
    }
}
