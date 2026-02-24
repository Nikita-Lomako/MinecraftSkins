using FluentValidation;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Validation;

public class SkinCreateDtoValidator : AbstractValidator<SkinCreateDto>
{
    public SkinCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BasePriceUsd)
            .GreaterThan(0)
            .LessThanOrEqualTo(9999.99m);

        RuleFor(x => x.IsAvailable)
            .NotNull();
    }
}

