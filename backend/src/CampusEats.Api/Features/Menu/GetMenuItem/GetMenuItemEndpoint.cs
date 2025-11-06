using MediatR;

namespace CampusEats.Api.Features.Menu.GetMenuItem;

public static class GetMenuItemEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var item = await mediator.Send(new GetMenuItemQuery(id));
                return item is null ? Results.NotFound() : Results.Ok(item);
            })
            .WithTags("Menu")
            .WithSummary("Get a menu item")
            .WithDescription("Gets a single menu item by id.");
    }
}