using FluentValidation;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Validation;

public class PurchaseCreateDtoValidator : AbstractValidator<PurchaseCreateDto>
{
    public PurchaseCreateDtoValidator()
    {
        RuleFor(x => x.SkinId)
            .NotEmpty()
            .NotEqual(Guid.Empty);
    }
}

