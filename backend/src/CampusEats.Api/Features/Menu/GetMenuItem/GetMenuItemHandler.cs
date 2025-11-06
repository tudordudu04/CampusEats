using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Menu.GetMenuItem;

public class GetMenuItemHandler(AppDbContext db) : IRequestHandler<GetMenuItemQuery, MenuItemDto?>
{
    public async Task<MenuItemDto?> Handle(GetMenuItemQuery request, CancellationToken ct)
    {
        return await db.MenuItems
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new MenuItemDto(x.Id, x.Name, x.Price, x.Description, x.Category, x.ImageUrl, x.Allergens))
            .FirstOrDefaultAsync(ct);
    }
}