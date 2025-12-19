using MediatR;

namespace CampusEats.Api.Features.Auth.UpdateUserProfile;

public record UpdateUserProfileCommand() : IRequest<IResult>
{
    public string? Name { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressNumber { get; init; }
    public string? AddressDetails { get; init; }
}