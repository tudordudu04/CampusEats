using CampusEats.Api.Enums;

namespace CampusEats.Api.Features.Menu;

public record MenuItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    MenuCategory Category,
    string? ImageUrl,
    string[] Allergens
);