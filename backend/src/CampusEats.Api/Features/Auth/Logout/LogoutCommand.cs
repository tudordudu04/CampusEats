using MediatR;

namespace CampusEats.Api.Features.Auth.Logout;

public record LogoutCommand() : IRequest<bool>;