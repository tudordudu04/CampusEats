using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Menu.UpdateMenuItem;

public class UpdateMenuItemHandler(AppDbContext db) : IRequestHandler<UpdateMenuItemCommand, bool>
{
    public async Task<bool> Handle(UpdateMenuItemCommand request, CancellationToken ct)
    {
        var exists = await db.MenuItems.AnyAsync(x => x.Id == request.Id, ct);
        if (!exists) return false;

        var entity = new MenuItem(
            request.Id,
            request.Name.Trim(),
            request.Price,
            request.Description?.Trim(),
            request.Category?.Trim(),
            request.ImageUrl?.Trim(),
            request.Allergens ?? []
        );

        db.MenuItems.Update(entity);
        var affected = await db.SaveChangesAsync(ct);
        return affected > 0;
    }
}