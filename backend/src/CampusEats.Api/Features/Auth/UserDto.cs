namespace CampusEats.Api.Features.Auth;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);