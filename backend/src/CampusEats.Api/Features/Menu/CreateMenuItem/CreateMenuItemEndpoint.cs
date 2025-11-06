using MediatR;

namespace CampusEats.Api.Features.Menu.CreateMenuItem;

public static class CreateMenuItemEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/menu", async (CreateMenuItemCommand cmd, IMediator mediator) =>
            {
                var id = await mediator.Send(cmd);
                return Results.Created($"/api/menu/{id}", new { id });
            })
            .WithTags("Menu")
            .WithSummary("Create a menu item") 
            .WithDescription("Creates a new menu item and returns its id.");
    }
}