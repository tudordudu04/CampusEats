using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Menu.GetAllMenuItems;

public class GetAllMenuItemsHandler(AppDbContext db)
    : IRequestHandler<GetAllMenuItemsQuery, IReadOnlyList<MenuItemDto>>
{
    public async Task<IReadOnlyList<MenuItemDto>> Handle(GetAllMenuItemsQuery request, CancellationToken ct)
    {
        var menuItems = await db.MenuItems.AsNoTracking().ToListAsync(ct);
        
        var menuItemIds = menuItems.Select(m => m.Id).ToList();
        
        var ratings = await db.MenuItemReviews
            .AsNoTracking()
            .Where(r => menuItemIds.Contains(r.MenuItemId))
            .GroupBy(r => r.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                AverageRating = g.Average(r => r.Rating),
                ReviewCount = g.Count()
            })
            .ToListAsync(ct);

        return menuItems.Select(m =>
        {
            var rating = ratings.FirstOrDefault(r => r.MenuItemId == m.Id);
            return new MenuItemDto(
                m.Id,
                m.Name,
                m.Price,
                m.Description,
                m.Category,
                m.ImageUrl,
                m.Allergens,
                rating != null ? Math.Round(rating.AverageRating, 1) : null,
                rating?.ReviewCount ?? 0
            );
        }).ToList();
    }
}
