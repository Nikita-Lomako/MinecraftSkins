using FluentValidation.TestHelper;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Validation;

namespace MinecraftSkins.Tests.UnitTests.Application.Validation;

public class SkinUpdateDtoValidatorTests
{
    private readonly SkinUpdateDtoValidator _validator;

    public SkinUpdateDtoValidatorTests()
    {
        _validator = new SkinUpdateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var dto = new SkinUpdateDto
        {
            Name = "Updated Skin",
            BasePriceUsd = 15.50m,
            IsAvailable = false
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
        var dto = new SkinUpdateDto
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
        var dto = new SkinUpdateDto
        {
            Name = new string('A', 101),
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
        var dto = new SkinUpdateDto
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
}
