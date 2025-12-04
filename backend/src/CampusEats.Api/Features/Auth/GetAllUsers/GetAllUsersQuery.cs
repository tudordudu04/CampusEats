using MediatR;

namespace CampusEats.Api.Features.Auth.GetAllUsers;

public record GetAllUsersQuery() : IRequest<IResult>;