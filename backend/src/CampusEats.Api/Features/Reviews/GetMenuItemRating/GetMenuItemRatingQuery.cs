using MediatR;

namespace CampusEats.Api.Features.Reviews.GetMenuItemRating;

public record GetMenuItemRatingQuery(Guid MenuItemId) : IRequest<MenuItemRatingDto>;
