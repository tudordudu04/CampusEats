using CampusEats.Api.Features.Menu.DeleteMenuItem;
using MediatR;

namespace CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;


public static class DeleteKitchenTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/kitchen/task/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteKitchenTaskCommand(id));
                return result;
            })
            .WithTags("Kitchen Task")
            .WithSummary("Delete a Kitchen Task")
            .WithDescription("Deletes a Kitchen Task by id.");
    }
}