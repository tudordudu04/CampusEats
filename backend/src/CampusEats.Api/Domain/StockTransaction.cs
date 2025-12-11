
namespace CampusEats.Api.Domain;

public enum StockTransactionType
{
    Restock,    // Delivery from supplier
    Usage,      // Consumed by an order
    Waste,      // Spoiled or dropped
    Adjustment  // Inventory count correction
}

public class StockTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IngredientId { get; set; }
    
    public Ingredient? Ingredient { get; set; }
    
    public decimal QuantityChanged { get; set; } // Positive for Restock, Negative for Usage/Waste
    
    public StockTransactionType Type { get; set; }
    public string? Note { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}