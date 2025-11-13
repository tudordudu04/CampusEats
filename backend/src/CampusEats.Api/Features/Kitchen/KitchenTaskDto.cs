using CampusEats.Api.Enums;

namespace CampusEats.Api.Features.Kitchen;

public record KitchenTaskDto(
    Guid Id, 
    Guid OrderId,
    Guid AssignedTo,
    string Status,
    string? Notes,
    DateTime UpdatedAt
);