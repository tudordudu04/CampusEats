using MediatR;

namespace CampusEats.Api.Features.Menu.UpdateMenuItem;

public record UpdateMenuItemCommand(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string? Category,
    string? ImageUrl,
    string[] Allergens
) : IRequest<bool>;