using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TapitAI.Domain.Common;
using TapitAI.Domain.Entities;
using TapitAI.Infrastructure.Data.Interceptors;
using TapitAI.Infrastructure.Identity;

namespace TapitAI.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    AuditableEntitySaveChangesInterceptor auditInterceptor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Media> MediaFiles => Set<Media>();
    public DbSet<UserDatingProfile> UserDatingProfiles => Set<UserDatingProfile>();
    public DbSet<ProfilePhoto> ProfilePhotos => Set<ProfilePhoto>();
    public DbSet<ProfileVideo> ProfileVideos => Set<ProfileVideo>();
    public DbSet<TapStatus> TapStatuses => Set<TapStatus>();
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<SpotlightSession> SpotlightSessions => Set<SpotlightSession>();
    public DbSet<SpotlightSessionFeed> SpotlightSessionFeeds => Set<SpotlightSessionFeed>();
    public DbSet<UserLike> UserLikes => Set<UserLike>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();
    public DbSet<LookupOption> LookupOptions => Set<LookupOption>();
    public DbSet<PlaceholderPhoto> PlaceholderPhotos => Set<PlaceholderPhoto>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplySoftDeleteFilters(builder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var method = typeof(AppDbContext)
                .GetMethod(nameof(BuildSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            var filter = (LambdaExpression)method.Invoke(null, null)!;
            builder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    private static LambdaExpression BuildSoftDeleteFilter<T>() where T : class, ISoftDeletable
    {
        Expression<Func<T, bool>> filter = e => !e.IsDeleted;
        return filter;
    }
}
