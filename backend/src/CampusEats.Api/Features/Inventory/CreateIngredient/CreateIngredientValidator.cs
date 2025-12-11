using FluentValidation;

namespace CampusEats.Api.Features.Inventory.CreateIngredient;

public class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
    }
}