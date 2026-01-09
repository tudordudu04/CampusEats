using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Reviews.AddReview;

public class AddReviewHandler(AppDbContext db) : IRequestHandler<AddReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(AddReviewCommand request, CancellationToken ct)
    {
        // Check if user already has a review for this menu item
        var existingReview = await db.MenuItemReviews
            .FirstOrDefaultAsync(r => r.MenuItemId == request.MenuItemId && r.UserId == request.UserId, ct);

        if (existingReview != null)
        {
            throw new InvalidOperationException("User already has a review for this menu item. Use update instead.");
        }

        // Validate rating range
        if (request.Rating < 1.0m || request.Rating > 5.0m)
        {
            throw new ArgumentException("Rating must be between 1.0 and 5.0");
        }

        // Verify menu item exists
        var menuItemExists = await db.MenuItems.AnyAsync(m => m.Id == request.MenuItemId, ct);
        if (!menuItemExists)
        {
            throw new InvalidOperationException("Menu item not found");
        }

        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = request.MenuItemId,
            UserId = request.UserId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.MenuItemReviews.Add(review);
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
