using MediatR;

namespace CampusEats.Api.Features.Reviews.UpdateReview;

public record UpdateReviewCommand(
    Guid ReviewId,
    Guid UserId,
    decimal Rating,
    string? Comment
) : IRequest<ReviewDto>;
