using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Payments.CreatePaymentSession;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Payments.ConfirmPayment;

public class ConfirmPaymentHandler(AppDbContext db) : IRequestHandler<ConfirmPaymentCommand>
{
    public async Task Handle(ConfirmPaymentCommand request, CancellationToken ct)
    {
        if (request.EventType != "checkout.session.completed")
            return;

        using var doc = JsonDocument.Parse(request.PayloadJson);
        var root = doc.RootElement;

        // Try to find metadata at root, otherwise look under data.object (event-wrapped)
        JsonElement metadataEl;
        if (root.TryGetProperty("metadata", out metadataEl) == false)
        {
            if (root.TryGetProperty("data", out var dataEl)
                && dataEl.ValueKind == JsonValueKind.Object
                && dataEl.TryGetProperty("object", out var objEl)
                && objEl.TryGetProperty("metadata", out metadataEl))
            {
              
            }
            else
            {
                throw new InvalidDataException("Missing `metadata` in payload or in `data.object`.");
            }
        }

        if (!metadataEl.TryGetProperty("payment_id", out var paymentIdEl) ||
            !metadataEl.TryGetProperty("user_id", out var userIdEl) ||
            !metadataEl.TryGetProperty("order_items", out var orderItemsEl))
        {
            throw new InvalidDataException("Required metadata keys (payment_id, user_id, order_items) are missing.");
        }

        var paymentIdRaw = paymentIdEl.GetString();
        var userIdRaw = userIdEl.GetString();
        var orderItemsJson = orderItemsEl.GetString();

        if (string.IsNullOrWhiteSpace(paymentIdRaw) || string.IsNullOrWhiteSpace(userIdRaw) || string.IsNullOrWhiteSpace(orderItemsJson))
            throw new InvalidDataException("One or more required metadata values are empty.");

        await HandleCheckoutCompleted(paymentIdRaw, userIdRaw, orderItemsJson, ct);
    }

    private async Task HandleCheckoutCompleted(string paymentIdRaw, string userIdRaw, string orderItemsJson, CancellationToken ct)
    {
        var paymentId = Guid.Parse(paymentIdRaw);
        var userId = Guid.Parse(userIdRaw);
        var orderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(orderItemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                         ?? new List<OrderItemDto>();

        var payment = await db.Payments.FindAsync(new object[] { paymentId }, ct);
        if (payment == null) return;

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = payment.Amount,
            CreatedAt = DateTime.UtcNow
        };

        var menuItemIds = orderItems.Select(x => Guid.Parse(x.MenuItemId)).ToList();
        var menuItems = await db.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        foreach (var item in orderItems)
        {
            if (!Guid.TryParse(item.MenuItemId, out var menuItemId)) continue;
            if (!menuItems.TryGetValue(menuItemId, out var menuItem)) continue;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = menuItem.Price
            });
        }

        db.Orders.Add(order);

        payment.OrderId = order.Id;
        payment.Status = PaymentStatus.SUCCEDED;
        payment.CompletedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}