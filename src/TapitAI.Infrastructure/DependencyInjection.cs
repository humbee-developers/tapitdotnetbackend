using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.BackgroundServices;
using TapitAI.Infrastructure.Data;
using TapitAI.Infrastructure.Data.Interceptors;
using TapitAI.Infrastructure.Hubs;
using TapitAI.Infrastructure.Identity;
using TapitAI.Infrastructure.Repositories;
using TapitAI.Infrastructure.Services;
using TapitAI.Infrastructure.Services.Cache;
using TapitAI.Infrastructure.Services.Chat;
using TapitAI.Infrastructure.Services.Discovery;
using TapitAI.Infrastructure.Services.Spotlight;
using TapitAI.Infrastructure.Services.Firebase;
using TapitAI.Infrastructure.Services.Media;
using TapitAI.Infrastructure.Services.Media.Providers;
using TapitAI.Infrastructure.Services.RealTime;
using TapitAI.Infrastructure.Services.Settings;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSettings(configuration);
        services.AddDatabase(configuration);
        services.AddIdentityAndAuth(configuration);
        services.AddRepositories();
        services.AddApplicationServices();
        services.AddStorageProviders(configuration);
        services.AddChatServices();
        services.AddCaching(configuration);
        services.AddSignalR();
        services.AddDatingServices();
        services.AddBackgroundServices();

        return services;
    }

    private static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<Auth0Settings>(configuration.GetSection(Auth0Settings.SectionName));
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.Configure<GetStreamSettings>(configuration.GetSection(GetStreamSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        services.Configure<FirebaseSettings>(configuration.GetSection(FirebaseSettings.SectionName));
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsql.UseNetTopologySuite();
                });
        });
    }

    private static void AddIdentityAndAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        var auth0Section = configuration.GetSection(Auth0Settings.SectionName);
        var auth0Settings = auth0Section.Get<Auth0Settings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "MultiScheme";
            options.DefaultChallengeScheme = "MultiScheme";
        })
        .AddJwtBearer("Admin", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddJwtBearer("Auth0", options =>
        {
            options.Authority = $"https://{auth0Settings.Domain}/";
            options.Audience = auth0Settings.Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Settings.Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Settings.Audience,
                ValidateLifetime = true
            };
            // Allow SignalR WebSocket auth via query string token
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        })
        .AddPolicyScheme("MultiScheme", "MultiScheme", options =>
        {
            options.ForwardDefaultSelector = _ => "Auth0";
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin").AddAuthenticationSchemes("Admin"));
            options.AddPolicy("UserOnly", policy =>
                policy.RequireRole("User").AddAuthenticationSchemes("Auth0"));
            options.AddPolicy("AdminOrUser", policy =>
                policy.RequireAuthenticatedUser().AddAuthenticationSchemes("Admin", "Auth0"));
        });

        services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformation>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
    }

    private static void AddStorageProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var storageSettings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>()!;

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var s3 = storageSettings.S3;
            var credentials = new BasicAWSCredentials(s3.AccessKey, s3.SecretKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region) };
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<IStorageProvider, S3StorageProvider>();
        services.AddSingleton<IStorageProvider, CloudinaryStorageProvider>();
        services.AddSingleton<IStorageProvider, GoogleStorageProvider>();
        services.AddSingleton<MediaStorageFactory>();
        services.AddScoped<IMediaStorageService, MediaStorageService>();
    }

    private static void AddChatServices(this IServiceCollection services)
    {
        services.AddSingleton<IChatService, StreamChatService>();
    }

    private static void AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()!;

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
            options.InstanceName = redisSettings.KeyPrefix;
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IAdminSettingService, AdminSettingService>();
    }

    private static void AddDatingServices(this IServiceCollection services)
    {
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<ISpotlightService, SpotlightService>();
        services.AddScoped<IFirebaseService, FirebaseService>();
        services.AddScoped<IRealTimeService, SignalRNotificationService>();
    }

    private static void AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<SampleBackgroundService>();
        services.AddHostedService<TapStatusResetBackgroundService>();
        services.AddHostedService<ConnectionExpiryBackgroundService>();
        services.AddHostedService<SpotlightGenerationBackgroundService>();
        services.AddHostedService<SystemConnectionBackgroundService>();
    }
}
