using FluentValidation;

namespace CampusEats.Api.Features.Inventory.AdjustStock;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(200);
    }
}