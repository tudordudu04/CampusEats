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

        var subtotal = request.Items.Sum(item => 
            menuItems[item.MenuItemId].Price * item.Quantity);

        decimal discountAmount = 0;
        Guid? userCouponId = null;
        string couponDescription = "";

        // Apply coupon if provided
        if (!string.IsNullOrWhiteSpace(request.UserCouponId) && Guid.TryParse(request.UserCouponId, out var parsedCouponId))
        {
            var userCoupon = await db.UserCoupons
                .Include(uc => uc.Coupon)
                .ThenInclude(c => c.SpecificMenuItem)
                .FirstOrDefaultAsync(uc => uc.Id == parsedCouponId && 
                                           uc.UserId == Guid.Parse(userId) && 
                                           !uc.IsUsed &&
                                           (uc.ExpiresAtUtc == null || uc.ExpiresAtUtc > DateTime.UtcNow), ct);

            if (userCoupon != null && userCoupon.Coupon.IsActive)
            {
                var coupon = userCoupon.Coupon;
                
                // Check minimum order amount
                if (!coupon.MinimumOrderAmount.HasValue || subtotal >= coupon.MinimumOrderAmount.Value)
                {
                    switch (coupon.Type)
                    {
                        case CampusEats.Api.Enums.CouponType.PercentageDiscount:
                            discountAmount = subtotal * (coupon.DiscountValue / 100m);
                            couponDescription = $" (Cupon: -{coupon.DiscountValue}%)";
                            break;
                        
                        case CampusEats.Api.Enums.CouponType.FixedAmountDiscount:
                            discountAmount = Math.Min(coupon.DiscountValue, subtotal);
                            couponDescription = $" (Cupon: -{coupon.DiscountValue} RON)";
                            break;
                        
                        case CampusEats.Api.Enums.CouponType.FreeItem:
                            if (coupon.SpecificMenuItemId.HasValue)
                            {
                                var itemDto = request.Items.FirstOrDefault(i => Guid.Parse(i.MenuItemId) == coupon.SpecificMenuItemId.Value);
                                if (itemDto != null && menuItems.TryGetValue(itemDto.MenuItemId, out var freeItem))
                                {
                                    discountAmount = freeItem.Price;
                                    couponDescription = $" (Cupon: {freeItem.Name} gratuit)";
                                }
                            }
                            break;
                    }
                    
                    userCouponId = parsedCouponId;
                }
            }
        }

        var totalAmount = Math.Max(0, subtotal - discountAmount);

        // Create a single line item with the final total (including discount if applicable)
        var lineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "ron",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Comandă CampusEats{couponDescription}",
                    },
                    UnitAmount = (long)(totalAmount * 100),
                },
                Quantity = 1,
            }
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userId),
            Amount = totalAmount,
            Currency = "ron",
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };

        var metadata = new Dictionary<string, string>
        {
            { "payment_id", payment.Id.ToString() },
            { "user_id", userId },
            { "order_items", System.Text.Json.JsonSerializer.Serialize(request.Items) },
            { "order_notes", request.Notes ?? "" }
        };

        if (userCouponId.HasValue)
        {
            metadata["user_coupon_id"] = userCouponId.Value.ToString();
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = config["Stripe:SuccessUrl"] ?? "https://campuseats.info",
            CancelUrl  = config["Stripe:CancelUrl"]  ?? "https://campuseats.info",
            Metadata = metadata
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        payment.StripeSessionId = session.Id;
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        return new CreatePaymentSessionResult(session.Id, session.Url);
    }
}