using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.GetUserReview;

public class GetUserReviewHandler(AppDbContext db)
    : IRequestHandler<GetUserReviewQuery, ReviewDto?>
{
    public async Task<ReviewDto?> Handle(GetUserReviewQuery request, CancellationToken ct)
    {
        var review = await db.MenuItemReviews
            .AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.MenuItemId == request.MenuItemId && r.UserId == request.UserId, ct);

        if (review == null)
            return null;

        return new ReviewDto(
            review.Id,
            review.MenuItemId,
            review.UserId,
            review.User!.Name,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc,
            review.UpdatedAtUtc
        );
    }
}
