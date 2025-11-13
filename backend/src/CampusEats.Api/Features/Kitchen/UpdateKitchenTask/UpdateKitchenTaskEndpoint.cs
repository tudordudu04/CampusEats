using MediatR;

namespace CampusEats.Api.Features.Kitchen.UpdateKitchenTask;

public static class UpdateKitchenTaskEndpoint
{
    public static void Map(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/kitchen/tasks/{id:guid}", async (
                Guid id,
                UpdateKitchenTaskCommand body,
                IMediator mediator) =>
            {
                var command = body with { Id = id };
                return await mediator.Send(command);
            })
            .WithTags("Kitchen Task")
            .WithName("Updates a Kitchen Task")
            .WithSummary("Updates a Kitchen Task’s status, assignee, or notes.");
    }
}