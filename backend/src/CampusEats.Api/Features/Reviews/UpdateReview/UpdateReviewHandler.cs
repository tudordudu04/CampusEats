using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.UpdateReview;

public class UpdateReviewHandler(AppDbContext db) : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken ct)
    {
        var review = await db.MenuItemReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found");
        }

        // Verify that the user owns this review
        if (review.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You can only update your own reviews");
        }

        // Validate rating range
        if (request.Rating < 1.0m || request.Rating > 5.0m)
        {
            throw new ArgumentException("Rating must be between 1.0 and 5.0");
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        review.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var user = await db.Users.FindAsync([review.UserId], ct);

        return new ReviewDto(
            review.Id,
            review.MenuItemId,
            review.UserId,
            user?.Name ?? "Unknown",
            review.Rating,
            review.Comment,
            review.CreatedAtUtc,
            review.UpdatedAtUtc
        );
    }
}
