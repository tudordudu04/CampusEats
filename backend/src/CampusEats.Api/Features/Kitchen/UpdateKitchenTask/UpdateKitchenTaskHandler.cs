using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Kitchen.UpdateKitchenTask;

public class UpdateKitchenTaskHandler(AppDbContext db)
    : IRequestHandler<UpdateKitchenTaskCommand, IResult>
{
    public async Task<IResult> Handle(UpdateKitchenTaskCommand request, CancellationToken ct)
    {
        // 1. Găsim Task-ul de bucătărie
        var kitchenTask = await db.KitchenTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (kitchenTask is null)
            return Results.NotFound($"Kitchen task {request.Id} not found.");

        // 2. Verificăm statusul comenzii părinte
        var parentOrder = await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == kitchenTask.OrderId, ct);

        // 3. Parsăm noul status dorit (dacă există în request)
        KitchenTaskStatus? newStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<KitchenTaskStatus>(request.Status, true, out var parsed))
                newStatus = parsed;
            else
                return Results.BadRequest("Invalid status value.");
        }

        // --- LOGICĂ SPECIALĂ: Comenzi Anulate ---
        if (parentOrder != null && parentOrder.Status == OrderStatus.Cancelled)
        {
            // Dacă comanda e anulată, permitem DOAR setarea pe 'Completed' (pentru a o ascunde din UI)
            if (newStatus == KitchenTaskStatus.Completed)
            {
                kitchenTask.Status = KitchenTaskStatus.Completed;
                kitchenTask.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return Results.Ok(kitchenTask); // Ieșim aici, nu atingem Order-ul
            }
            
            // Orice altă acțiune este blocată
            return Results.BadRequest("Această comandă a fost anulată. Poți doar să o elimini (Completed).");
        }
        // ----------------------------------------

        // Actualizări standard pentru comenzi active
        if (request.AssignedTo is not null && request.AssignedTo != Guid.Empty)
            kitchenTask.AssignedTo = request.AssignedTo.Value;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            kitchenTask.Notes = request.Notes.Trim();

        if (newStatus.HasValue)
        {
            kitchenTask.Status = newStatus.Value;

            // Sincronizare cu Order (doar dacă nu e anulată - verificat mai sus)
            if (parentOrder != null)
            {
                // Reatașăm pentru update
                db.Orders.Attach(parentOrder);
                
                switch (newStatus.Value)
                {
                    case KitchenTaskStatus.Preparing:
                        parentOrder.Status = OrderStatus.Preparing;
                        break;
                    case KitchenTaskStatus.Ready:
                    case KitchenTaskStatus.Completed:
                        parentOrder.Status = OrderStatus.Completed;
                        break;
                }
                parentOrder.UpdatedAt = DateTime.UtcNow;
            }
        }

        kitchenTask.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(kitchenTask);
    }
}