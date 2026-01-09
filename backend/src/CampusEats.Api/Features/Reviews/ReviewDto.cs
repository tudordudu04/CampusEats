namespace CampusEats.Api.Features.Reviews;

public record ReviewDto(
    Guid Id,
    Guid MenuItemId,
    Guid UserId,
    string UserName,
    decimal Rating,
    string? Comment,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record MenuItemRatingDto(
    Guid MenuItemId,
    decimal AverageRating,
    int TotalReviews
);
