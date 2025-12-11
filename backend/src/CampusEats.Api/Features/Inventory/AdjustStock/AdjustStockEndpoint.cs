using MediatR;

namespace CampusEats.Api.Features.Inventory.AdjustStock;

public static class AdjustStockEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/inventory/adjust", async (AdjustStockCommand command, IMediator mediator) =>
        {
            return await mediator.Send(command);
        })
        .WithTags("Kitchen Inventory")
        .WithSummary("Adjust stock level")
        .WithDescription("Use this to Restock (add) or Log Waste (remove).");
    }
}