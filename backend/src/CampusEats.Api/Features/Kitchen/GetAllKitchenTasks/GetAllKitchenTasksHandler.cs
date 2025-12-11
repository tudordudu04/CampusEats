using CampusEats.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;

public class GetAllKitchenTasksHandler(AppDbContext db)
    : IRequestHandler<GetAllKitchenTasksQuery, IResult>
{
    public async Task<IResult> Handle(GetAllKitchenTasksQuery request, CancellationToken ct)
    {
        var items = await (from task in db.KitchenTasks
                join order in db.Orders on task.OrderId equals order.Id
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

        // Returnăm OK cu lista (chiar dacă e goală)
        return Results.Ok(items);
    }
}