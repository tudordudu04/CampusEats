using MediatR;

namespace CampusEats.Api.Features.Reviews.GetMenuItemReviews;

public record GetMenuItemReviewsQuery(Guid MenuItemId) : IRequest<IReadOnlyList<ReviewDto>>;
