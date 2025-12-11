
namespace CampusEats.Api.Domain;

public class Ingredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    public string Unit { get; set; } = string.Empty;
    
    public decimal CurrentStock { get; set; }
    
    public decimal LowStockThreshold { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}