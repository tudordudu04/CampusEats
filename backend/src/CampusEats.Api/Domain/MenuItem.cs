using CampusEats.Api.Enums;

namespace CampusEats.Api.Domain;

public record MenuItem(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    MenuCategory Category,
    string? ImageUrl,
    string[] Allergens
);