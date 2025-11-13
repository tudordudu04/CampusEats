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
        var entity = await db.KitchenTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (entity is null)
            return Results.NotFound($"Kitchen task {request.Id} not found.");

        if (request.AssignedTo is not null && request.AssignedTo != Guid.Empty)
            entity.AssignedTo = request.AssignedTo.Value;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            entity.Notes = request.Notes.Trim();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<KitchenTaskStatus>(request.Status, true, out var status))
                return Results.BadRequest("Invalid status value.");

            entity.Status = status;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(entity);
    }
}
