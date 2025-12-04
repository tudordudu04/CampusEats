using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Security.Claims;

namespace CampusEats.Api.Features.Payments.CreatePaymentSession;

public class CreatePaymentSessionHandler(
    AppDbContext db,
    IConfiguration config,
    IHttpContextAccessor httpContext)
    : IRequestHandler<CreatePaymentSessionCommand, CreatePaymentSessionResult>
{
    public async Task<CreatePaymentSessionResult> Handle(CreatePaymentSessionCommand request, CancellationToken ct)
    {
        var userId = httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();

        var menuItemIds = request.Items.Select(x => Guid.Parse(x.MenuItemId)).ToList();
        var menuItems = await db.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id.ToString(), ct);

        var lineItems = request.Items.Select(item =>
        {
            var menuItem = menuItems[item.MenuItemId];
            return new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "ron",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = menuItem.Name,
                    },
                    UnitAmount = (long)(menuItem.Price * 100),
                },
                Quantity = item.Quantity,
            };
        }).ToList();

        var totalAmount = request.Items.Sum(item => 
            menuItems[item.MenuItemId].Price * item.Quantity);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userId),
            Amount = totalAmount,
            Currency = "ron",
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = config["Stripe:SuccessUrl"] ?? "http://localhost:5173/orders?status=success",
            CancelUrl  = config["Stripe:CancelUrl"]  ?? "http://localhost:5173/orders?status=cancel",

            Metadata = new Dictionary<string, string>
            {
                { "payment_id", payment.Id.ToString() },
                { "user_id", userId },
                { "order_items", System.Text.Json.JsonSerializer.Serialize(request.Items) },
                // Salvăm și notițele aici
                { "order_notes", request.Notes ?? "" }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        payment.StripeSessionId = session.Id;
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        return new CreatePaymentSessionResult(session.Id, session.Url);
    }
}