using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Infrastructure.Data;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly AppDbContext DbContext;
    protected readonly UserManager<IdentityUser> UserManager;
    protected readonly RoleManager<IdentityRole> RoleManager;
    protected readonly IServiceScope Scope;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        UserManager = Scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        RoleManager = Scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    }

    public virtual async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    public virtual ValueTask DisposeAsync()
    {
        Scope?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected async Task<IdentityUser> CreateTestUserAsync(
        string userName = "testuser",
        string password = "Test123!",
        CancellationToken cancellationToken = default)
    {
        var user = new IdentityUser { UserName = userName, Email = $"{userName}@test.com" };
        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await UserManager.IsInRoleAsync(user, "User"))
        {
            await UserManager.AddToRoleAsync(user, "User");
        }

        return user;
    }

    protected async Task<string> GetAuthTokenAsync(
        string userName = "testuser",
        string password = "Test123!",
        CancellationToken cancellationToken = default)
    {
        var loginRequest = new LoginRequestDto { UserName = userName, Password = password };
        var response = await Client.PostAsJsonAsync("/api/login", loginRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Login failed with status {(int)response.StatusCode}: {body}");
        }
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
        return loginResponse?.Token ?? throw new Exception("Failed to get auth token");
    }

    protected async Task<Domain.Models.Skin> CreateTestSkinAsync(
        string? name = null,
        decimal? basePriceUsd = null,
        bool isAvailable = true,
        CancellationToken cancellationToken = default)
    {
        var skin = TestDataFactory.CreateSkin(
            name: name ?? $"Test Skin {Guid.NewGuid():N}",
            basePriceUsd: basePriceUsd ?? 10m,
            isAvailable: isAvailable);
        
        DbContext.Skins.Add(skin);
        await DbContext.SaveChangesAsync(cancellationToken);
        return skin;
    }

    protected async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        DbContext.Purchases.RemoveRange(DbContext.Purchases);
        DbContext.Skins.RemoveRange(DbContext.Skins);
        var users = await DbContext.Users.ToListAsync(cancellationToken);
        DbContext.Users.RemoveRange(users);
        await DbContext.SaveChangesAsync(cancellationToken);

        if (!await RoleManager.RoleExistsAsync("User"))
        {
            await RoleManager.CreateAsync(new IdentityRole("User"));
        }

        if (!await RoleManager.RoleExistsAsync("Admin"))
        {
            await RoleManager.CreateAsync(new IdentityRole("Admin"));
        }
    }
}
