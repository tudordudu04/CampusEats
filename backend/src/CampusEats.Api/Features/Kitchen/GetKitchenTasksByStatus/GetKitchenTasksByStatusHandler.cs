using CampusEats.Api.Data;
using CampusEats.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;

public class GetKitchenTasksByStatusHandler(AppDbContext db) : IRequestHandler<GetKitchenTasksByStatusQuery, IResult>
{
    public async Task<IResult> Handle(GetKitchenTasksByStatusQuery request, CancellationToken ct)
    {
        var statusParsed = Enum.TryParse<KitchenTaskStatus>(request.Status, true, out var status);
        if (!statusParsed)
            return Results.BadRequest($"Invalid status: {request.Status}");

        var tasks = await (from task in db.KitchenTasks
                join order in db.Orders on task.OrderId equals order.Id
                where task.Status == status
                select new KitchenTaskDto(
                    task.Id,
                    task.OrderId,
                    task.AssignedTo,
                    task.Status.ToString(),
                    task.Notes,
                    task.UpdatedAt,
                    order.Status.ToString() 
                ))
            .AsNoTracking()
            .ToListAsync(ct);

        return Results.Ok(tasks);
    }
}