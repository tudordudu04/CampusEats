using FluentValidation;

namespace CampusEats.Api.Features.Inventory.GetStockByName;

public class GetStockByNameValidator : AbstractValidator<GetStockByNameCommand>
{
    public GetStockByNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingredient name is required.")
            .MaximumLength(100).WithMessage("Ingredient name cannot exceed 100 characters.");
    }
}