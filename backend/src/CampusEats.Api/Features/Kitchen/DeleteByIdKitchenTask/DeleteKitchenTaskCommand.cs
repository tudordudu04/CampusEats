using MediatR;

namespace CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;

public record DeleteKitchenTaskCommand(Guid Id) : IRequest<IResult>;