using CampusEats.Api.Domain;
using MediatR;

namespace CampusEats.Api.Features.Inventory.AdjustStock;

public record AdjustStockCommand(Guid IngredientId, decimal Quantity, StockTransactionType Type, string? Note) : IRequest<IResult>;