using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Menu.GetMenuItem;

public class GetMenuItemHandler(AppDbContext db) : IRequestHandler<GetMenuItemQuery, MenuItemDto?>
{
    public async Task<MenuItemDto?> Handle(GetMenuItemQuery request, CancellationToken ct)
    {
        var menuItem = await db.MenuItems
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(ct);

        if (menuItem == null)
            return null;

        var rating = await db.MenuItemReviews
            .AsNoTracking()
            .Where(r => r.MenuItemId == menuItem.Id)
            .GroupBy(r => r.MenuItemId)
            .Select(g => new
            {
                AverageRating = g.Average(r => r.Rating),
                ReviewCount = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        return new MenuItemDto(
            menuItem.Id,
            menuItem.Name,
            menuItem.Price,
            menuItem.Description,
            menuItem.Category,
            menuItem.ImageUrl,
            menuItem.Allergens,
            rating != null ? Math.Round(rating.AverageRating, 1) : null,
            rating?.ReviewCount ?? 0
        );
    }
}