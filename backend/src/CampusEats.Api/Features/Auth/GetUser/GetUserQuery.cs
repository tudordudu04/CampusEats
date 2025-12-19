using MediatR;
namespace CampusEats.Api.Features.Auth.GetUser;

public record GetUserQuery(Guid UserId) : IRequest<UserDto?>;
