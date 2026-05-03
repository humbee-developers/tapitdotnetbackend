using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Data.Seeds;

public static class DatingAppSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedAdminSettingsAsync(db);
        await SeedLookupOptionsAsync(db);
        await SeedPlaceholderPhotosAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminSettingsAsync(AppDbContext db)
    {
        var defaults = new[]
        {
            (AdminSettingKeys.DiscoveryRadiusMiles, "100", "double", "Discovery radius shown on map (miles)", "Discovery"),
            (AdminSettingKeys.ConnectionsPerDayLimit, "3", "int", "Max established connections per user per day", "Connection"),
            (AdminSettingKeys.ConnectionRequestsSendPerDay, "5", "int", "Max connection requests a user can send per day", "Connection"),
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

    private static async Task SeedLookupOptionsAsync(AppDbContext db)
    {
        if (db.Set<LookupOption>().Any()) return;

        var options = new List<LookupOption>
        {
            // AgeRange
            LookupOption.Create(LookupCategory.AgeRange, "18-24", 1, true),
            LookupOption.Create(LookupCategory.AgeRange, "25-34", 2),
            LookupOption.Create(LookupCategory.AgeRange, "35-44", 3),
            LookupOption.Create(LookupCategory.AgeRange, "45+", 4),

            // Gender
            LookupOption.Create(LookupCategory.Gender, "Male", 1, true),
            LookupOption.Create(LookupCategory.Gender, "Female", 2),
            LookupOption.Create(LookupCategory.Gender, "Non-binary", 3),
            LookupOption.Create(LookupCategory.Gender, "Other", 4),

            // Lifestyle
            LookupOption.Create(LookupCategory.Lifestyle, "Fitness", 1),
            LookupOption.Create(LookupCategory.Lifestyle, "Creative", 2),
            LookupOption.Create(LookupCategory.Lifestyle, "Foodie", 3),
            LookupOption.Create(LookupCategory.Lifestyle, "Traveler", 4),
            LookupOption.Create(LookupCategory.Lifestyle, "Homebody", 5),
            LookupOption.Create(LookupCategory.Lifestyle, "Adventurer", 6),
            LookupOption.Create(LookupCategory.Lifestyle, "Social", 7),
            LookupOption.Create(LookupCategory.Lifestyle, "Intellectual", 8),

            // LookingFor
            LookupOption.Create(LookupCategory.LookingFor, "Dating", 1, true),
            LookupOption.Create(LookupCategory.LookingFor, "Casual", 2),
            LookupOption.Create(LookupCategory.LookingFor, "Friendship", 3),
            LookupOption.Create(LookupCategory.LookingFor, "Long-term Relationship", 4),
            LookupOption.Create(LookupCategory.LookingFor, "Something New", 5),

            // PreferHeight
            LookupOption.Create(LookupCategory.PreferHeight, "Any Height", 1, true),
            LookupOption.Create(LookupCategory.PreferHeight, "Taller", 2),
            LookupOption.Create(LookupCategory.PreferHeight, "Shorter", 3),
            LookupOption.Create(LookupCategory.PreferHeight, "Same Height", 4),
        };

        db.Set<LookupOption>().AddRange(options);
    }

    private static async Task SeedPlaceholderPhotosAsync(AppDbContext db)
    {
        if (db.Set<PlaceholderPhoto>().Any()) return;

        var photos = new List<PlaceholderPhoto>();

        for (var i = 1; i <= 8; i++)
            photos.Add(PlaceholderPhoto.Create("Male", $"https://randomuser.me/api/portraits/men/{i + 10}.jpg", i));

        for (var i = 1; i <= 8; i++)
            photos.Add(PlaceholderPhoto.Create("Female", $"https://randomuser.me/api/portraits/women/{i + 10}.jpg", i));

        for (var i = 1; i <= 5; i++)
            photos.Add(PlaceholderPhoto.Create("Non-binary", $"https://randomuser.me/api/portraits/lego/{i}.jpg", i));

        db.Set<PlaceholderPhoto>().AddRange(photos);
    }
}
