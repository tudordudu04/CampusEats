using MediatR;

namespace CampusEats.Api.Features.Reviews.GetUserReview;

public record GetUserReviewQuery(Guid MenuItemId, Guid UserId) : IRequest<ReviewDto?>;
