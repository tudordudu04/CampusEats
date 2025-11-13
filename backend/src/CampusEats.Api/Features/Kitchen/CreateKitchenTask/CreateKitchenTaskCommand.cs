using CampusEats.Api.Enums;
using MediatR;

namespace CampusEats.Api.Features.Kitchen.CreateKitchenTask;

public record CreateKitchenTaskCommand(
    Guid OrderId,
    Guid AssignedTo,
    string? Notes
) : IRequest<IResult>;