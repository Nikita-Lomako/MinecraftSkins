using FluentValidation.TestHelper;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Validation;

namespace MinecraftSkins.Tests.UnitTests.Application.Validation;

public class PurchaseCreateDtoValidatorTests
{
    private readonly PurchaseCreateDtoValidator _validator;

    public PurchaseCreateDtoValidatorTests()
    {
        _validator = new PurchaseCreateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidSkinId_ShouldPass()
    {
        // Arrange
        var dto = new PurchaseCreateDto
        {
            SkinId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyGuid_ShouldFail()
    {
        // Arrange
        var dto = new PurchaseCreateDto
        {
            SkinId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SkinId);
    }

    [Fact]
    public void Validate_WithDefaultGuid_ShouldFail()
    {
        // Arrange
        var dto = new PurchaseCreateDto
        {
            SkinId = default(Guid)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SkinId);
    }
}
