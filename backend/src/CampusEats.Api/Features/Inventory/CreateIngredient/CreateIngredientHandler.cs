using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using FluentValidation;
using MediatR;

namespace CampusEats.Api.Features.Inventory.CreateIngredient;

public class CreateIngredientHandler(AppDbContext context, IValidator<CreateIngredientCommand> validator)
    : IRequestHandler<CreateIngredientCommand, IResult>
{
    public async Task<IResult> Handle(CreateIngredientCommand request, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var ingredient = new Ingredient
        {
            Name = request.Name,
            Unit = request.Unit,
            LowStockThreshold = request.LowStockThreshold,
            CurrentStock = 0 
        };

        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(ct);

        return Results.Ok(new { ingredient.Id, ingredient.Name });
    }
}