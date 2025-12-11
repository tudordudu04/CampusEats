using MediatR;

namespace CampusEats.Api.Features.Inventory.GetStockByName;

public static class GetStockByNameEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory/{name}", async (string name, IMediator mediator) =>
        {
            return await mediator.Send(new GetStockByNameCommand(name));
        })
        .WithTags("Kitchen Inventory")
        .WithSummary("Get stock by name")
        .WithDescription("Returns the stock level and unit for a specific ingredient.");
    }
}