using CampusEats.Api.Data;
using MediatR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using CampusEats.Api.Domain;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Enums;

namespace CampusEats.Api.Features.Orders.GetOrders;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuerry, OrderDto?>
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public GetOrdersHandler(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<OrderDto?> Handle(GetOrdersQuerry request, CancellationToken cancellationToken)
    {
        var user = _http.HttpContext?.User;
        if (user == null) return null;

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId)) return null;

        // --- MODIFICARE AICI: Permitem accesul și pentru WORKER ---
        var canViewAll = user.IsInRole(UserRole.MANAGER.ToString())
                      || user.IsInRole(UserRole.WORKER.ToString())
                      || user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == UserRole.MANAGER.ToString());

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null) return null;
        
        // Verificăm dacă userul are dreptul să vadă comanda
        if (!canViewAll && order.UserId != userId) return null;

        var menuIds = order.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuNames = await _db.MenuItems
            .AsNoTracking()
            .Where(mi => menuIds.Contains(mi.Id))
            .ToDictionaryAsync(mi => mi.Id, mi => mi.Name, cancellationToken);

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status,
            Total = order.Total,
            CreatedAtUtc = order.CreatedAt,
            UpdatedAtUtc = order.UpdatedAt,
            Notes = order.Notes,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                MenuItemName = menuNames.TryGetValue(i.MenuItemId, out var n) ? n : null
            }).ToList()
        };
    }
}