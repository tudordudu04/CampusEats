namespace CampusEats.Api.Domain;

public record MenuItem(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string? Category,
    string? ImageUrl,
    string[] Allergens
);