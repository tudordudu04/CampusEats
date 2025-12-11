using MediatR;

namespace CampusEats.Api.Features.Inventory.CreateIngredient;

public record CreateIngredientCommand(string Name, string Unit, decimal LowStockThreshold) : IRequest<IResult>;