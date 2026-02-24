using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;
using MinecraftSkins.Infrastructure.Repositories;
using MinecraftSkins.Tests.IntegrationTests;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.IntegrationTests.Repositories;

public class SkinRepositoryTests : IntegrationTestBase
{
    private readonly SkinRepository _repository;

    public SkinRepositoryTests(CustomWebApplicationFactory factory) : base(factory)
    {
        var logger = Factory.Services.GetRequiredService<ILogger<SkinRepository>>();
        _repository = new SkinRepository(DbContext, logger);
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WithValidSkin_CreatesSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = TestDataFactory.CreateSkin(name: $"Test Skin {Guid.NewGuid():N}", isAvailable: true);

        // Act
        await _repository.CreateAsync(skin, ct);

        // Assert
        var created = await DbContext.Skins.FirstOrDefaultAsync(s => s.Id == skin.Id, ct);
        created.Should().NotBeNull();
        created!.Name.Should().Be(skin.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(name: "Test Skin", cancellationToken: ct);

        // Act
        var result = await _repository.GetByIdAsync(skin.Id, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(skin.Id);
        result.Name.Should().Be("Test Skin");
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedSkin_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(cancellationToken: ct);
        await _repository.DeleteAsync(skin.Id, ct);

        // Act
        var result = await _repository.GetByIdAsync(skin.Id, ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdIncludingDeletedAsync_WithDeletedSkin_ReturnsSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(cancellationToken: ct);
        await _repository.DeleteAsync(skin.Id, ct);

        // Act
        var result = await _repository.GetByIdIncludingDeletedAsync(skin.Id, ct);

        // Assert
        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithAvailableOnlyFilter_ReturnsOnlyAvailable()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        await CreateTestSkinAsync(name: "Available 1", isAvailable: true, cancellationToken: ct);
        await CreateTestSkinAsync(name: "Available 2", isAvailable: true, cancellationToken: ct);
        await CreateTestSkinAsync(name: "Unavailable", isAvailable: false, cancellationToken: ct);

        // Act
        var result = await _repository.GetAllAsync(availableOnly: true, null, 0, 10, ct);

        // Assert
        result.Count.Should().Be(2);
        result.All(s => s.IsAvailable).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithValidSkin_UpdatesSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(name: "Original Name", cancellationToken: ct);
        skin.Name = "Updated Name";
        skin.BasePriceUsd = 20m;

        // Act
        await _repository.UpdateAsync(skin, ct);

        // Assert
        var updated = await DbContext.Skins.FirstOrDefaultAsync(s => s.Id == skin.Id, ct);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.BasePriceUsd.Should().Be(20m);
    }
}

