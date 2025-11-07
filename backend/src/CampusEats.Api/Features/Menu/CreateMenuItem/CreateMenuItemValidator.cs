using FluentValidation;

namespace CampusEats.Api.Features.Menu.CreateMenuItem;

public class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(400).When(x => x.Description != null);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.ImageUrl).MaximumLength(500).When(x => x.ImageUrl != null);
    }
}