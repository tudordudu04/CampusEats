using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using MediatR;

namespace CampusEats.Api.Features.Menu.CreateMenuItem;

public class CreateMenuItemHandler(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateMenuItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateMenuItemCommand request, CancellationToken ct)
    {
        var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? GetDefaultImageUrl(request.Category)
            : request.ImageUrl.Trim();
        
        var entity = new MenuItem
        (
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Price,
            request.Description?.Trim(),
            request.Category,
            imageUrl,
            request.Allergens ?? []
        );

        db.MenuItems.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
    private string? GetDefaultImageUrl(MenuCategory category)
    {
        if (category == MenuCategory.OTHER)
            return null;

        var httpContext = httpContextAccessor.HttpContext;
        var baseUrl = httpContext != null
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : string.Empty;

        var fileName = category switch
        {
            MenuCategory.PIZZA   => "pizza.png",
            MenuCategory.BURGER  => "burger.png",
            MenuCategory.SALAD   => "salad.png",
            MenuCategory.SOUP    => "soup.png",
            MenuCategory.DESSERT => "dessert.png",
            MenuCategory.DRINK   => "drink.png",
            _                    => null
        };

        if (fileName is null)
            return null;

        return string.IsNullOrEmpty(baseUrl)
            ? $"/menu-images/defaults/{fileName}"
            : $"{baseUrl}/menu-images/defaults/{fileName}";
    }
}