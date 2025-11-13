using MediatR;

namespace CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;

public record GetKitchenTasksByStatusQuery(string Status) : IRequest<IResult>;
