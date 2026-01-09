using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.GetMenuItemRating;

public class GetMenuItemRatingHandler(AppDbContext db)
    : IRequestHandler<GetMenuItemRatingQuery, MenuItemRatingDto>
{
    public async Task<MenuItemRatingDto> Handle(GetMenuItemRatingQuery request, CancellationToken ct)
    {
        var reviews = await db.MenuItemReviews
            .AsNoTracking()
            .Where(r => r.MenuItemId == request.MenuItemId)
            .ToListAsync(ct);

        if (!reviews.Any())
        {
            return new MenuItemRatingDto(request.MenuItemId, 0, 0);
        }

        var averageRating = reviews.Average(r => r.Rating);

        return new MenuItemRatingDto(
            request.MenuItemId,
            Math.Round(averageRating, 1),
            reviews.Count
        );
    }
}
