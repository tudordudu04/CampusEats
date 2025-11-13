using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Kitchen.CreateKitchenTask;

public class CreateKitchenTaskHandler(
    AppDbContext db,
    IValidator<CreateKitchenTaskCommand> validator,
    ILogger<CreateKitchenTaskHandler> logger
) : IRequestHandler<CreateKitchenTaskCommand, IResult>
{
    public async Task<IResult> Handle(CreateKitchenTaskCommand request, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        // pentru a crea un kitchen task valid este nevoie de un order id valid(prezent in baza de date)
        // si de un id al unui utilizator valid
        //
        // var orderExists = await db.Orders.AnyAsync(o => o.Id == request.OrderId, ct);
        // if (!orderExists)
        //     return Results.BadRequest($"Order {request.OrderId} not found.");
        //
        // var userExists = await db.Users.AnyAsync(u => u.Id == request.AssignedTo, ct);
        // if (!userExists)
        //     return Results.BadRequest($"User {request.AssignedTo} not found.");

        var entity = new KitchenTask(
            Guid.NewGuid(),
            request.OrderId,
            request.AssignedTo,
            KitchenTaskStatus.NotStarted,
            request.Notes,
            DateTime.UtcNow
        );

        db.KitchenTasks.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created KitchenTask {TaskId} for Order {OrderId}", entity.Id, entity.OrderId);

        return Results.Created($"/api/kitchen/tasks/{entity.Id}", entity);
    }
}