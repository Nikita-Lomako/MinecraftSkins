using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MinecraftSkins.Infrastructure.Data.Extensions;

/// <summary>
/// Seeds default users (TestUser, Admin, TestUser2) at application startup.
/// Cannot be done in ModelBuilder.Seed() because that runs at model-build time (no UserManager),
/// and passwords must be hashed with Identity at runtime. Call EnsureSeedUsersAsync after Migrate().
/// </summary>
public static class DataSeeder
{
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
}
