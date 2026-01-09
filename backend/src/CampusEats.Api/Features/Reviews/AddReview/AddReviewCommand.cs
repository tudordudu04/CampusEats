using MediatR;

namespace CampusEats.Api.Features.Reviews.AddReview;

public record AddReviewCommand(
    Guid MenuItemId,
    Guid UserId,
    decimal Rating,
    string? Comment
) : IRequest<ReviewDto>;
