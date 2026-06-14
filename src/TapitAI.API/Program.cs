using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TapitAI.API.Extensions;
using TapitAI.API.Infrastructure;
using TapitAI.API.Middleware;
using TapitAI.Application;
using TapitAI.Infrastructure;
using TapitAI.Infrastructure.Data;
using TapitAI.Infrastructure.Data.Seeds;
using TapitAI.Infrastructure.Hubs;
using TapitAI.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer())));
builder.Services.AddRouting(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerWithAuth();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});


// ── App Pipeline ───────────────────────────────────────────────────────────────
var app = builder.Build();

await SeedDatabaseAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TapitAI API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ConnectionHub>("/hubs/connection");

app.Run();

// ── Seed ───────────────────────────────────────────────────────────────────────
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Create database if it doesn't exist, then enable PostGIS
    var connStr = app.Configuration.GetConnectionString("DefaultConnection")!;
    var parsed = new NpgsqlConnectionStringBuilder(connStr);
    var dbName = parsed.Database!;
    var adminConnStr = connStr.Replace($"Database={dbName}", "Database=postgres",
        StringComparison.OrdinalIgnoreCase);
    await using (var adminConn = new NpgsqlConnection(adminConnStr))
    {
        await adminConn.OpenAsync();
        var exists = await new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = $1",
            adminConn) { Parameters = { new() { Value = dbName } } }
            .ExecuteScalarAsync();
        if (exists is null)
            await new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", adminConn).ExecuteNonQueryAsync();
    }
    await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS postgis;");
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole(role));
    }

    var config = app.Configuration;
    var adminEmail = config["AdminCredentials:DefaultEmail"] ?? "admin@tapitai.com";
    var adminPassword = config["AdminCredentials:DefaultPassword"] ?? "Admin@123456";

    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Admin"
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    await DatingAppSeeder.SeedAsync(db);
}
