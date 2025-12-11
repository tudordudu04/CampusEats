using CampusEats.Api.Data;
using CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Inventory.GetStockByName;

public class GetStockByNameHandler(AppDbContext context, IValidator<GetStockByNameCommand> validator)
    : IRequestHandler<GetStockByNameCommand, IResult>
{
    public async Task<IResult> Handle(GetStockByNameCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var ingredient = await context.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (ingredient == null)
        {
            return Results.NotFound($"Ingredient '{request.Name}' not found.");
        }

        return Results.Ok(new IngredientStockDto(
            ingredient.Id, 
            ingredient.Name, 
            ingredient.CurrentStock, 
            ingredient.Unit, 
            ingredient.LowStockThreshold, 
            ingredient.UpdatedAt.ToShortDateString()));
    }
}