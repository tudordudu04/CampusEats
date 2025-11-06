using MediatR;

namespace CampusEats.Api.Features.Menu.GetAllMenuItems;

public record GetAllMenuItemsQuery() : IRequest<IReadOnlyList<MenuItemDto>>;