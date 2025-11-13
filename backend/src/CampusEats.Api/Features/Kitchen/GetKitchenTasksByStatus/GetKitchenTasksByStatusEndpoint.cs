using MediatR;

namespace CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;

public static class GetKitchenTasksByStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/kitchen/tasks/{status}", async (string status, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetKitchenTasksByStatusQuery(status));
                return result;
            })
        .WithTags("Kitchen Task")
        .WithSummary("Get kitchen tasks by status")
        .WithDescription("Returns all kitchen tasks filtered by their status.");
    }
}
