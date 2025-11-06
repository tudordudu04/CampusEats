using MediatR;

namespace CampusEats.Api.Features.Menu.GetAllMenuItems;

public static class GetAllMenuItemsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu", async (IMediator mediator) =>
            {
                var items = await mediator.Send(new GetAllMenuItemsQuery());
                return Results.Ok(items);
            })
            .WithTags("Menu")
            .WithSummary("Get all menu items");
    }
}