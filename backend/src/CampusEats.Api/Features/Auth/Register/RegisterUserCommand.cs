using CampusEats.Api.Enums;
using MediatR;

namespace CampusEats.Api.Features.Auth.Register;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role
) : IRequest<IResult>;