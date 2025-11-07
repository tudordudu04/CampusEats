using FluentValidation;

namespace CampusEats.Api.Features.Menu.UpdateMenuItem;

public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(400).When(x => x.Description != null);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.ImageUrl).MaximumLength(500).When(x => x.ImageUrl != null);
    }
}