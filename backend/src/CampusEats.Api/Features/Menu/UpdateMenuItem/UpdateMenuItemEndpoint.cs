using MediatR;

namespace CampusEats.Api.Features.Menu.UpdateMenuItem;

public static class UpdateMenuItemEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/{id:guid}", async (Guid id, UpdateMenuItemCommand cmd, IMediator mediator) =>
            {
                if (id != cmd.Id) return Results.BadRequest(new { error = "Route id and body id do not match." });

                var updated = await mediator.Send(cmd);
                return updated ? Results.Ok("Updated") : Results.NotFound();
            })
            .WithTags("Menu")
            .WithSummary("Update a menu item")
            .WithDescription("Updates a menu item by id.");
    }
}