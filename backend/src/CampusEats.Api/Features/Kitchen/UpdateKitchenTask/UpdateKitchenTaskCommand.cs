using MediatR;
using Microsoft.AspNetCore.Http;

namespace CampusEats.Api.Features.Kitchen.UpdateKitchenTask;

public record UpdateKitchenTaskCommand(
    Guid Id,
    Guid? AssignedTo,
    string? Status,
    string? Notes
) : IRequest<IResult>;