using MediatR;

namespace CampusEats.Api.Features.Menu.CreateMenuItem;

public record CreateMenuItemCommand(
    string Name,
    decimal Price,
    string? Description,
    string? Category,
    string? ImageUrl,
    string[] Allergens
) : IRequest<Guid>;