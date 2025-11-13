using FluentValidation;

namespace CampusEats.Api.Features.Kitchen.CreateKitchenTask;

public class CreateKitchenTaskValidator : AbstractValidator<CreateKitchenTaskCommand>
{
    public CreateKitchenTaskValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.AssignedTo)
            .NotEmpty().WithMessage("AssignedTo is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(100).WithMessage("Notes cannot exceed 100 characters.");
    }
}
