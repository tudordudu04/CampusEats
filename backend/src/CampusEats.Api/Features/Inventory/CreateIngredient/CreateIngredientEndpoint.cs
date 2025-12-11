using MediatR;

namespace CampusEats.Api.Features.Inventory.CreateIngredient;

public static class CreateIngredientEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/ingredients", async (CreateIngredientCommand command, IMediator mediator) =>
        {
            return await mediator.Send(command);
        })
        .WithTags("Kitchen Inventory")
        .WithSummary("Create a new ingredient")
        .WithDescription("Defines a new ingredient to be tracked (e.g. 'Tomato', 'Bun'). Starts with 0 stock.");
    }
}