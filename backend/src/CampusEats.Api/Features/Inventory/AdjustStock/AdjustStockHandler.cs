using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using FluentValidation;
using MediatR;

namespace CampusEats.Api.Features.Inventory.AdjustStock;

public class AdjustStockHandler(AppDbContext context, IValidator<AdjustStockCommand> validator)
    : IRequestHandler<AdjustStockCommand, IResult>
{
    public async Task<IResult> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }
        
        var ingredient = await context.Ingredients.FindAsync(new object[] { request.IngredientId }, ct);
        if (ingredient == null)
        {
            return Results.NotFound("Ingredient not found.");
        }
        
        decimal changeAmount = request.Quantity;
        
        // Deduct for Usage or Waste
        if (request.Type == StockTransactionType.Usage || request.Type == StockTransactionType.Waste)
        {
            changeAmount = -request.Quantity;
        }
        // Prevent negative stock
        if (ingredient.CurrentStock + changeAmount < 0)
        {
             return Results.BadRequest("Insufficient stock for this operation.");
        }

        ingredient.CurrentStock += changeAmount;
        if (changeAmount > 0)
        {
            Console.WriteLine(DateTime.UtcNow);
            ingredient.UpdatedAt = DateTime.UtcNow;
        }

        var transaction = new StockTransaction
        {
            IngredientId = ingredient.Id,
            Type = request.Type,
            QuantityChanged = changeAmount,
            Note = request.Note
        };

        context.StockTransactions.Add(transaction);
        await context.SaveChangesAsync(ct);

        return Results.Ok(new { ingredient.Id, ingredient.CurrentStock, Message = "Stock updated successfully." });
    }
}