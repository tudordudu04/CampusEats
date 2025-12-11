
namespace CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;

using MediatR;

public class GetAllIngredientsInStockCommand : IRequest<List<IngredientStockDto>>
{
}