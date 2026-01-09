namespace CampusEats.Api.Domain;

public class MenuItemReview
{
    public Guid Id { get; init; }
    public Guid MenuItemId { get; set; }
    public Guid UserId { get; set; }
    public decimal Rating { get; set; } // 1.0 to 5.0
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Navigation properties
    public MenuItem? MenuItem { get; set; }
    public User? User { get; set; }
}
