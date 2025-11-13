using CampusEats.Api.Data;
using CampusEats.Api.Features.Kitchen.CreateKitchenTask;
using MediatR;

namespace CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;

public class DeleteKitchenTaskHandler(AppDbContext db) : IRequestHandler<DeleteKitchenTaskCommand, IResult>
{
    public async Task<IResult> Handle(DeleteKitchenTaskCommand request, CancellationToken ct)
    {
        var entity = await db.KitchenTasks.FindAsync([request.Id], ct);
        if (entity is null)
            return Results.NotFound($"Kitchen task with Id {request.Id} not found");

        db.KitchenTasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        
        return Results.Ok($"Kitchen task with id {request.Id} deleted");
    }
}