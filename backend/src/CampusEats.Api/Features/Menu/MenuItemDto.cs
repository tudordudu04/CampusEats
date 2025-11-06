namespace CampusEats.Api.Features.Menu;

public record MenuItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string? Category,
    string? ImageUrl,
    string[] Allergens
);