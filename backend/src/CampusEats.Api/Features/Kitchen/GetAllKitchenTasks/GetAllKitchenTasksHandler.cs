using CampusEats.Api.Data;
using CampusEats.Api.Features.Menu;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;

public class GetAllKitchenTasksHandler(AppDbContext db)
    : IRequestHandler<GetAllKitchenTasksQuery, IResult>
{
    public async Task<IResult> Handle(GetAllKitchenTasksQuery request, CancellationToken ct)
    {
        var items = await db.KitchenTasks
            .AsNoTracking()
            .Select(t => new KitchenTaskDto(
                t.Id,
                t.OrderId,
                t.AssignedTo,
                t.Status.ToString(),
                t.Notes,
                t.UpdatedAt
            ))
            .ToListAsync(ct);

        return items.Count == 0 ? Results.NoContent() : Results.Ok(items);
    }
}