using CampusEats.Api.Data;
using MediatR;

namespace CampusEats.Api.Features.Menu.DeleteMenuItem;

public class DeleteMenuItemHandler(AppDbContext db) : IRequestHandler<DeleteMenuItemCommand, bool>
{
    public async Task<bool> Handle(DeleteMenuItemCommand request, CancellationToken ct)
    {
        var entity = await db.MenuItems.FindAsync([request.Id], ct);
        if (entity is null) return false;

        db.MenuItems.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}