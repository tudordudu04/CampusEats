using CampusEats.Api.Infrastructure.Auth;
using MediatR;

namespace CampusEats.Api.Features.Auth.Login;

public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<AuthResultDto>;