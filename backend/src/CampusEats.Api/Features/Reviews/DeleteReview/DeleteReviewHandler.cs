using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.DeleteReview;

public class DeleteReviewHandler(AppDbContext db) : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken ct)
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
            throw new UnauthorizedAccessException("You can only delete your own reviews");
        }

        db.MenuItemReviews.Remove(review);
        await db.SaveChangesAsync(ct);
    }
}
