using CampusEats.Api.Enums;

namespace CampusEats.Api.Domain;

public class KitchenTask
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid AssignedTo { get; set; }
    public KitchenTaskStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public KitchenTask(Guid id, Guid orderId, Guid assignedTo, KitchenTaskStatus status, string? notes, DateTime updatedAt)
    {
        Id = id;
        OrderId = orderId;
        AssignedTo = assignedTo;
        Status = status;
        Notes = notes;
        UpdatedAt = updatedAt;
    }

    public KitchenTask() { }
}
