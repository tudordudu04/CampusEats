using MediatR;

namespace CampusEats.Api.Features.Menu.GetMenuItem;

public record GetMenuItemQuery(Guid Id) : IRequest<MenuItemDto?>;