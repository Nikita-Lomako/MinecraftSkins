using FluentValidation;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Validation;

public class SkinUpdateDtoValidator : AbstractValidator<SkinUpdateDto>
{
    public SkinUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.BasePriceUsd)
            .GreaterThan(0);

        RuleFor(x => x.IsAvailable)
            .NotNull();
    }
}

