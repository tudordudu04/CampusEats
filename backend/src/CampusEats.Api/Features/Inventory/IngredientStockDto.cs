namespace CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;

public record IngredientStockDto(
    Guid Id, 
    string Name, 
    decimal CurrentStock, 
    string Unit, 
    decimal LowStockThreshold, 
    string UpdatedAt
);