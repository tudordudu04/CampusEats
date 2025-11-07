using CampusEats.Api.Enums;
using MediatR;

namespace CampusEats.Api.Features.Menu.UpdateMenuItem;

public record UpdateMenuItemCommand(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    MenuCategory Category,
    string? ImageUrl,
    string[] Allergens
) : IRequest<bool>;