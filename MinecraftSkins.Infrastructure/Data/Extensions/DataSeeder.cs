using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;
using System.Text.Json;

namespace MinecraftSkins.Infrastructure.Data.Extensions;

/// <summary>
/// Seeds default users (TestUser, Admin, TestUser2) at application startup.
/// Cannot be done in ModelBuilder.Seed() because that runs at model-build time (no UserManager),
/// and passwords must be hashed with Identity at runtime. Call EnsureSeedUsersAsync after Migrate().
/// </summary>
public static class DataSeeder
{
    public static async Task EnsureSeedSkinsAsync(
    DbContext dbContext,
    ILogger? logger = null,
    CancellationToken cancellationToken = default)
    {
        if (dbContext is not AppDbContext appDbContext)
        {
            return;
        }

        if (await appDbContext.Skins.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "skins.seed.json");

        // Добавь fallback путь для Docker/разных окружений
        if (!File.Exists(seedPath))
        {
            seedPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "skins.seed.json");
        }

        if (!File.Exists(seedPath))
        {
            logger?.LogWarning("Skins seed file not found: {SeedPath}", seedPath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath, cancellationToken);

        // ✅ Добавь JsonSerializerOptions с PropertyNameCaseInsensitive
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var seedRows = JsonSerializer.Deserialize<List<SkinSeedModel>>(json, options) ?? new List<SkinSeedModel>();

        if (seedRows.Count == 0)
        {
            logger?.LogWarning("No skins in seed file");
            return;
        }

        // Добавь логирование для отладки
        logger?.LogInformation("Loaded {Count} skins from seed file. First skin ID: {FirstId}",
            seedRows.Count, seedRows.FirstOrDefault()?.Id);

        var skins = seedRows.Select(s => {
            try
            {
                return new Skin
                {
                    Id = Guid.Parse(s.Id),
                    Name = s.Name,
                    BasePriceUsd = s.PriceUsd,
                    IsAvailable = s.IsAvailable,
                    CreatedAtUtc = DateTime.SpecifyKind(s.CreatedAtUtc, DateTimeKind.Utc),
                    IsDeleted = false
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to parse skin seed row: Id={Id}, Name={Name}", s.Id, s.Name);
                throw;
            }
        }).ToList();

        await appDbContext.Skins.AddRangeAsync(skins, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Seeded {Count} skins from JSON", skins.Count);
    }

    public static async Task EnsureSeedUsersAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var users = new[]
        {
            (UserName: "TestUser", Password: "Password123!", Role: "User"),
            (UserName: "Admin", Password: "Admin123!", Role: "Admin"),
            (UserName: "TestUser2", Password: "TestUser123!", Role: "User"),
        };

        foreach (var (userName, password, roleName) in users)
        {
            if (await userManager.FindByNameAsync(userName) != null)
            {
                logger?.LogDebug("User {UserName} already exists, skipping", userName);
                continue;
            }

            var user = new IdentityUser
            {
                UserName = userName,
                NormalizedUserName = userManager.NormalizeName(userName),
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger?.LogWarning("Failed to create user {UserName}: {Errors}",
                    userName, string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                await userManager.AddToRoleAsync(user, roleName);
                logger?.LogInformation("Created user {UserName} with role {Role}", userName, roleName);
            }
            else
            {
                logger?.LogWarning("Role {Role} not found, user {UserName} created without role", roleName, userName);
            }
        }
    }

    private sealed class SkinSeedModel
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("priceUsd")]
        public decimal PriceUsd { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }
    }
}
