using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;

public class GetAllIngredientsInStockHandler(AppDbContext context)
    : IRequestHandler<GetAllIngredientsInStockCommand, List<IngredientStockDto>>
{
    public async Task<List<IngredientStockDto>> Handle(GetAllIngredientsInStockCommand request, CancellationToken cancellationToken)
    {
        return await context.Ingredients
            .AsNoTracking()
            .Select(i => new IngredientStockDto(i.Id, i.Name, i.CurrentStock, i.Unit, i.LowStockThreshold, i.UpdatedAt.ToShortDateString()))
            .ToListAsync(cancellationToken);
    }
}