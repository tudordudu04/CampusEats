using MediatR;

namespace CampusEats.Api.Features.Auth.DeleteUser;

public record DeleteUserCommand(Guid UserId) : IRequest<IResult>;