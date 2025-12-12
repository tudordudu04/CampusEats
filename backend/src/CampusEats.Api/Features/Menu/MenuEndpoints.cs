using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using CampusEats.Api.Features.Menu.GetMenuItem;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using CampusEats.Api.Features.Menu.UploadMenuImage;
using MediatR;

namespace CampusEats.Api.Features.Menu;

public static class MenuEndpoints
{
    public static void MapMenu(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/menu", async (CreateMenuItemCommand cmd, IMediator mediator) =>
            {
                var id = await mediator.Send(cmd);
                return Results.Created($"/api/menu/{id}", new { id });
            })
            .WithTags("Menu")
            .WithSummary("Create a menu item") 
            .WithDescription("Creates a new menu item and returns its id.");
        
        app.MapDelete("/api/menu/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var deleted = await mediator.Send(new DeleteMenuItemCommand(id));
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithTags("Menu")
            .WithSummary("Delete a menu item")
            .WithDescription("Deletes a menu item by id.");
        
        app.MapGet("/api/menu", async (IMediator mediator) =>
            {
                var items = await mediator.Send(new GetAllMenuItemsQuery());
                return Results.Ok(items);
            })
            .WithTags("Menu")
            .WithSummary("Get all menu items");
        
        app.MapGet("/api/menu/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var item = await mediator.Send(new GetMenuItemQuery(id));
                return item is null ? Results.NotFound() : Results.Ok(item);
            })
            .WithTags("Menu")
            .WithSummary("Get a menu item")
            .WithDescription("Gets a single menu item by id.");
        
        app.MapPut("/api/menu/{id:guid}", async (Guid id, UpdateMenuItemCommand cmd, IMediator mediator) =>
            {
                if (id != cmd.Id) return Results.BadRequest(new { error = "Route id and body id do not match." });

                var updated = await mediator.Send(cmd);
                return updated ? Results.Ok("Updated") : Results.NotFound();
            })
            .WithTags("Menu")
            .WithSummary("Update a menu item")
            .WithDescription("Updates a menu item by id.");
        
        app.MapPost("/api/menu/images", async (HttpRequest request, IMediator mediator) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest("Content type must be multipart/form-data.");

                var form = await request.ReadFormAsync();
                var file = form.Files["file"];

                if (file is null || file.Length == 0)
                    return Results.BadRequest("No file uploaded.");

                await using var stream = file.OpenReadStream();

                var cmd = new UploadMenuImageCommand(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream
                );

                var result = await mediator.Send(cmd);
                return Results.Ok(result);
            })
            .WithTags("Menu")
            .WithSummary("Upload a menu image")
            .WithDescription("Uploads a menu image and returns its URL.");
    }
}