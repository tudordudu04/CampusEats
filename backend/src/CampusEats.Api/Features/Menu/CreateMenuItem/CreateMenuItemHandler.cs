using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using MediatR;

namespace CampusEats.Api.Features.Menu.CreateMenuItem;

public class CreateMenuItemHandler(AppDbContext db) : IRequestHandler<CreateMenuItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateMenuItemCommand request, CancellationToken ct)
    {
        var entity = new MenuItem
        (
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Price,
            request.Description?.Trim(),
            request.Category,
            request.ImageUrl?.Trim(),
            request.Allergens ?? []
        );

        db.MenuItems.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}