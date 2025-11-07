using CampusEats.Api.Enums;
using CampusEats.Api.Infrastructure.Auth;
using MediatR;

namespace CampusEats.Api.Features.Auth.Register;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role
) : IRequest<AuthResultDto>;