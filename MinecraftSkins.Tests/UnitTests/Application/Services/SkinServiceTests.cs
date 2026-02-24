using AutoMapper;
using FluentValidation;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class SkinServiceTests
{
    private readonly ISkinRepository _skinRepository;
    private readonly IBtcRateService _btcRateService;
    private readonly IPriceCalculator _priceCalculator;
    private readonly IMapper _mapper;
    private readonly IValidator<SkinCreateDto> _createValidator;
    private readonly IValidator<SkinUpdateDto> _updateValidator;
    private readonly ILogger<SkinService> _logger;
    private readonly SkinService _service;
    private readonly Fixture _fixture;

    public SkinServiceTests()
    {
        _skinRepository = Substitute.For<ISkinRepository>();
        _btcRateService = Substitute.For<IBtcRateService>();
        _priceCalculator = Substitute.For<IPriceCalculator>();
        _mapper = CreateMapper();
        _createValidator = Substitute.For<IValidator<SkinCreateDto>>();
        _updateValidator = Substitute.For<IValidator<SkinUpdateDto>>();
        _logger = Substitute.For<ILogger<SkinService>>();
        _service = new SkinService(
            _skinRepository,
            _btcRateService,
            _priceCalculator,
            _mapper,
            _createValidator,
            _updateValidator,
            _logger);
        _fixture = new Fixture();
    }

    private static IMapper CreateMapper()
    {
        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MinecraftSkins.Application.MappingConfig>();
        var config = new MapperConfiguration(configExpression, LoggerFactory.Create(_ => { }));
        return config.CreateMapper();
    }

    [Fact]
    public async Task GetAllSkinsAsync_ReturnsListOfSkins()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skins = TestDataFactory.CreateSkins(5);
        _skinRepository.GetAllAsync(null, null, 0, 10, Arg.Any<CancellationToken>())
            .Returns(skins);
        _btcRateService.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(TestDataFactory.CreateBtcRateResult(70000m));
        _priceCalculator.CalculateFinalPrice(Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(10.5m);

        // Act
        var result = await _service.GetAllSkinsAsync(null, null, 0, 10, ct);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(5);
    }

    [Fact]
    public async Task GetSkinByIdAsync_WithValidId_ReturnsSkinDto()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var skin = TestDataFactory.CreateSkin(id: skinId);
        var btcRate = TestDataFactory.CreateBtcRateResult(70000m);

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);
        _btcRateService.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(btcRate);
        _priceCalculator.CalculateFinalPrice(skin.BasePriceUsd, btcRate.Rate)
            .Returns(10.5m);

        // Act
        var result = await _service.GetSkinByIdAsync(skinId, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(skinId);
        result.FinalPrice.Should().Be(10.5m);
    }

    [Fact]
    public async Task GetSkinByIdAsync_WithInvalidId_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns((Skin?)null);

        // Act
        var result = await _service.GetSkinByIdAsync(skinId, ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSkinAsync_WithValidDto_ReturnsCreatedSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var createDto = new SkinCreateDto
        {
            Name = "Test Skin",
            BasePriceUsd = 10m,
            IsAvailable = true
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        _createValidator.ValidateAsync(createDto, Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var result = await _service.CreateSkinAsync(createDto, ct);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        await _skinRepository.Received(1).CreateAsync(Arg.Any<Skin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSkinAsync_WithInvalidDto_ThrowsArgumentException()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var createDto = new SkinCreateDto { Name = "" };
        var validationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("Name", "Name is required") });
        _createValidator.ValidateAsync(createDto, Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act & Assert
        var act = () => _service.CreateSkinAsync(createDto, ct);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSkinAsync_WithValidDto_ReturnsUpdatedSkin()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var existingSkin = TestDataFactory.CreateSkin(id: skinId);
        var updateDto = new SkinUpdateDto
        {
            Name = "Updated Skin",
            BasePriceUsd = 15m,
            IsAvailable = false
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(existingSkin);
        _updateValidator.ValidateAsync(updateDto, Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var result = await _service.UpdateSkinAsync(skinId, updateDto, ct);

        // Assert
        result.Should().NotBeNull();
        await _skinRepository.Received(1).UpdateAsync(Arg.Any<Skin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSkinAsync_WithValidId_ReturnsTrue()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var skin = TestDataFactory.CreateSkin(id: skinId);
        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);

        // Act
        var result = await _service.DeleteSkinAsync(skinId, ct);

        // Assert
        result.Should().BeTrue();
        await _skinRepository.Received(1).DeleteAsync(skinId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSkinAsync_WithInvalidId_ReturnsFalse()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns((Skin?)null);

        // Act
        var result = await _service.DeleteSkinAsync(skinId, ct);

        // Assert
        result.Should().BeFalse();
        await _skinRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSkinsAsync_WhenBtcRateUnavailable_ReturnsSkinsWithoutPrice()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skins = TestDataFactory.CreateSkins(3);
        _skinRepository.GetAllAsync(null, null, 0, 10, Arg.Any<CancellationToken>())
            .Returns(skins);
        _btcRateService.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Rate unavailable"));

        // Act
        var result = await _service.GetAllSkinsAsync(null, null, 0, 10, ct);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.All(s => s.FinalPrice == null).Should().BeTrue();
    }
}
