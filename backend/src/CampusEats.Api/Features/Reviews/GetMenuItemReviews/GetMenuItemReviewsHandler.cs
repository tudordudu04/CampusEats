using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.GetMenuItemReviews;

public class GetMenuItemReviewsHandler(AppDbContext db)
    : IRequestHandler<GetMenuItemReviewsQuery, IReadOnlyList<ReviewDto>>
{
    public async Task<IReadOnlyList<ReviewDto>> Handle(GetMenuItemReviewsQuery request, CancellationToken ct)
    {
        return await db.MenuItemReviews
            .AsNoTracking()
            .Where(r => r.MenuItemId == request.MenuItemId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewDto(
                r.Id,
                r.MenuItemId,
                r.UserId,
                r.User!.Name,
                r.Rating,
                r.Comment,
                r.CreatedAtUtc,
                r.UpdatedAtUtc
            ))
            .ToListAsync(ct);
    }
}
