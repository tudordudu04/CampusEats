using MediatR;

namespace CampusEats.Api.Features.Menu.DeleteMenuItem;

public static class DeleteMenuItemEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/menu/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var deleted = await mediator.Send(new DeleteMenuItemCommand(id));
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithTags("Menu")
            .WithSummary("Delete a menu item")
            .WithDescription("Deletes a menu item by id.");
    }
}