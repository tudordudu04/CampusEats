namespace CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;
using MediatR;

public static class GetAllKitchenTasksEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/kitchen/tasks", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllKitchenTasksQuery());
                return result;
            })
            .WithTags("Kitchen Task")
            .WithSummary("Gets all Kitchen Tasks.")
            .WithDescription("Gets all Kitchen Tasks or NoContent if there are none.");
    }
}