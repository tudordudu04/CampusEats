using MediatR;

namespace CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;

public static class GetAllIngredientsInStockEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory", async (IMediator mediator) =>
        {
            return await mediator.Send(new GetAllIngredientsInStockCommand());
        })
        .WithTags("Kitchen Inventory")
        .WithSummary("Get all ingredients stock")
        .WithDescription("Returns a list of all ingredients with their current stock levels.");
    }
}