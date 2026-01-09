using MediatR;

namespace CampusEats.Api.Features.Reviews.DeleteReview;

public record DeleteReviewCommand(
    Guid ReviewId,
    Guid UserId
) : IRequest;
