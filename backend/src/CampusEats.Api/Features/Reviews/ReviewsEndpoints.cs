using CampusEats.Api.Features.Reviews.AddReview;
using CampusEats.Api.Features.Reviews.UpdateReview;
using CampusEats.Api.Features.Reviews.DeleteReview;
using CampusEats.Api.Features.Reviews.GetMenuItemReviews;
using CampusEats.Api.Features.Reviews.GetUserReview;
using CampusEats.Api.Features.Reviews.GetMenuItemRating;
using MediatR;
using System.Security.Claims;

namespace CampusEats.Api.Features.Reviews;

public static class ReviewsEndpoints
{
    public static void MapReviews(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reviews", async (AddReviewRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var cmd = new AddReviewCommand(
                        request.MenuItemId,
                        userId,
                        request.Rating,
                        request.Comment
                    );
                    var review = await mediator.Send(cmd);
                    return Results.Created($"/api/reviews/{review.Id}", review);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithTags("Reviews")
            .WithSummary("Add a review to a menu item");

        app.MapPut("/api/reviews/{reviewId:guid}", async (Guid reviewId, UpdateReviewRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var cmd = new UpdateReviewCommand(
                        reviewId,
                        userId,
                        request.Rating,
                        request.Comment
                    );
                    var review = await mediator.Send(cmd);
                    return Results.Ok(review);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Forbid();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithTags("Reviews")
            .WithSummary("Update your review");

        app.MapDelete("/api/reviews/{reviewId:guid}", async (Guid reviewId, ClaimsPrincipal user, IMediator mediator) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    await mediator.Send(new DeleteReviewCommand(reviewId, userId));
                    return Results.NoContent();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .RequireAuthorization()
            .WithTags("Reviews")
            .WithSummary("Delete your review");

        app.MapGet("/api/menu/{menuItemId:guid}/reviews", async (Guid menuItemId, IMediator mediator) =>
            {
                var reviews = await mediator.Send(new GetMenuItemReviewsQuery(menuItemId));
                return Results.Ok(reviews);
            })
            .WithTags("Reviews")
            .WithSummary("Get all reviews for a menu item");

        app.MapGet("/api/menu/{menuItemId:guid}/reviews/mine", async (Guid menuItemId, ClaimsPrincipal user, IMediator mediator) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var review = await mediator.Send(new GetUserReviewQuery(menuItemId, userId));
                return review is null ? Results.NotFound() : Results.Ok(review);
            })
            .RequireAuthorization()
            .WithTags("Reviews")
            .WithSummary("Get your review for a menu item");

        app.MapGet("/api/menu/{menuItemId:guid}/rating", async (Guid menuItemId, IMediator mediator) =>
            {
                var rating = await mediator.Send(new GetMenuItemRatingQuery(menuItemId));
                return Results.Ok(rating);
            })
            .WithTags("Reviews")
            .WithSummary("Get average rating for a menu item");
    }
}

public record AddReviewRequest(
    Guid MenuItemId,
    decimal Rating,
    string? Comment
);

public record UpdateReviewRequest(
    decimal Rating,
    string? Comment
);
