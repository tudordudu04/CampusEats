using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Menu.GetAllMenuItems;

public class GetAllMenuItemsHandler(AppDbContext db)
    : IRequestHandler<GetAllMenuItemsQuery, IReadOnlyList<MenuItemDto>>
{
    public async Task<IReadOnlyList<MenuItemDto>> Handle(GetAllMenuItemsQuery request, CancellationToken ct)
    {
        return await db.MenuItems
            .AsNoTracking()
            .Select(m => new MenuItemDto(
                m.Id,
                m.Name,
                m.Price,
                m.Description,
                m.Category,
                m.ImageUrl,
                m.Allergens))
            .ToListAsync(ct);
    }
}