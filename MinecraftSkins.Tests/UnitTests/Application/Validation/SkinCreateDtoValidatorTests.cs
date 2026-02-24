using FluentValidation.TestHelper;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Validation;

namespace MinecraftSkins.Tests.UnitTests.Application.Validation;

public class SkinCreateDtoValidatorTests
{
    private readonly SkinCreateDtoValidator _validator;

    public SkinCreateDtoValidatorTests()
    {
        _validator = new SkinCreateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = "Test Skin",
            BasePriceUsd = 10.50m,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = "",
            BasePriceUsd = 10m,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = new string('A', 101), // 101 символ
            BasePriceUsd = 10m,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithZeroPrice_ShouldFail()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = "Test Skin",
            BasePriceUsd = 0m,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BasePriceUsd);
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = "Test Skin",
            BasePriceUsd = -10m,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BasePriceUsd);
    }

    [Fact]
    public void Validate_WithPriceTooHigh_ShouldFail()
    {
        // Arrange
        var dto = new SkinCreateDto
        {
            Name = "Test Skin",
            BasePriceUsd = 10000m, // Превышает лимит 9999.99
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BasePriceUsd);
    }
}
