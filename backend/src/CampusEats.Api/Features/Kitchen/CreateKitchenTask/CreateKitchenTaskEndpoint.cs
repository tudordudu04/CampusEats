using MediatR;

namespace CampusEats.Api.Features.Kitchen.CreateKitchenTask;

public static class CreateKitchenTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/kitchen/tasks", async (CreateKitchenTaskCommand cmd, IMediator mediator) =>
            {
                var result = await mediator.Send(cmd);
                return result;
            })
            .WithTags("Kitchen Task")
            .WithSummary("Create a Kitchen Task") 
            .WithDescription("Creates a new Kitchen Task.");
    }
}