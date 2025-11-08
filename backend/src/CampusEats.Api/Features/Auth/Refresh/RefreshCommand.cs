using MediatR;

namespace CampusEats.Api.Features.Auth.Refresh;

public record RefreshCommand() : IRequest<string>;