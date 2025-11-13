using FluentValidation;

namespace CampusEats.Api.Features.Kitchen.UpdateKitchenTask;

public class UpdateKitchenTaskValidator : AbstractValidator<UpdateKitchenTaskCommand>
{
    public UpdateKitchenTaskValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .WithMessage("Invalid status value.");
    }

    private bool BeValidStatus(string? status)
        => status != null && Enum.TryParse<Enums.KitchenTaskStatus>(status, true, out _);
}