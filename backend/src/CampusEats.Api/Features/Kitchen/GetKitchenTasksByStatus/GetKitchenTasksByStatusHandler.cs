using CampusEats.Api.Data;
using CampusEats.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;

public class GetKitchenTasksByStatusHandler(AppDbContext db) : IRequestHandler<GetKitchenTasksByStatusQuery, IResult>
{
    public async Task<IResult> Handle(GetKitchenTasksByStatusQuery request, CancellationToken ct)
    {
        var statusParsed = Enum.TryParse<KitchenTaskStatus>(request.Status, true, out var status);
        if (!statusParsed)
            return Results.BadRequest($"Invalid status: {request.Status}");

        var tasks = await db.KitchenTasks
            .Where(t => t.Status == status)
            .Select(t => new KitchenTaskDto(
                t.Id,
                t.OrderId,
                t.AssignedTo,
                t.Status.ToString(),
                t.Notes,
                t.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(tasks);
    }
}