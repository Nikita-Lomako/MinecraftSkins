using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Tests.IntegrationTests;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.IntegrationTests.Endpoints;

public class SkinEndpointsIntegrationTests : IntegrationTestBase
{
    public SkinEndpointsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetAllSkins_ReturnsListOfSkins()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        await CreateTestSkinAsync(name: "Skin 1", isAvailable: true, cancellationToken: ct);
        await CreateTestSkinAsync(name: "Skin 2", isAvailable: true, cancellationToken: ct);

        // Act
        var response = await Client.GetAsync("/api/skins", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var skins = await response.Content.ReadFromJsonAsync<List<SkinDto>>(ct);
        skins.Should().NotBeNull();
        skins!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSkinById_WithValidId_ReturnsSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(name: "Test Skin", isAvailable: true, cancellationToken: ct);

        // Act
        var response = await Client.GetAsync($"/api/skins/{skin.Id}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SkinDto>(ct);
        result.Should().NotBeNull();
        result!.Id.Should().Be(skin.Id);
        result.Name.Should().Be("Test Skin");
    }

    [Fact]
    public async Task GetSkinById_WithInvalidId_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync($"/api/skins/{Guid.NewGuid()}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSkin_WithAdminRole_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var admin = await CreateTestUserAsync("admin", "Admin123!", ct);
        await UserManager.AddToRoleAsync(admin, "Admin");
        var token = await GetAuthTokenAsync("admin", "Admin123!", ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new SkinCreateDto
        {
            Name = "New Skin",
            BasePriceUsd = 15.50m,
            IsAvailable = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/skins", createDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var skin = await response.Content.ReadFromJsonAsync<SkinDto>(ct);
        skin.Should().NotBeNull();
        skin!.Name.Should().Be("New Skin");
    }

    [Fact]
    public async Task CreateSkin_WithoutAdminRole_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new SkinCreateDto
        {
            Name = "New Skin",
            BasePriceUsd = 15.50m,
            IsAvailable = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/skins", createDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSkin_WithAdminRole_ReturnsUpdatedSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var admin = await CreateTestUserAsync("admin2", "Admin123!", ct);
        await UserManager.AddToRoleAsync(admin, "Admin");
        var token = await GetAuthTokenAsync("admin2", "Admin123!", ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var skin = await CreateTestSkinAsync(name: "Original Name", isAvailable: true, cancellationToken: ct);
        var updateDto = new SkinUpdateDto
        {
            Name = "Updated Name",
            BasePriceUsd = 20m,
            IsAvailable = false
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/skins/{skin.Id}", updateDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<SkinDto>(ct);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.BasePriceUsd.Should().Be(20m);
        updated.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSkin_WithAdminRole_ReturnsNoContent()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var admin = await CreateTestUserAsync("admin3", "Admin123!", ct);
        await UserManager.AddToRoleAsync(admin, "Admin");
        var token = await GetAuthTokenAsync("admin3", "Admin123!", ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);

        // Act
        var response = await Client.DeleteAsync($"/api/skins/{skin.Id}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Проверяем, что скин soft-deleted
        var getResponse = await Client.GetAsync($"/api/skins/{skin.Id}", ct);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
