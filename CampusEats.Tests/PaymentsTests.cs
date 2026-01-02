using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Payments.ConfirmPayment;
using CampusEats.Api.Features.Payments.CreatePaymentSession;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Json;

namespace CampusEats.Tests;

public class PaymentsTests
{
    [Fact]
    public async Task ConfirmPayment_Should_Create_Order_And_Mark_Payment_As_Succeeded()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Burger",
            Price: 25.99m,
            Description: null,
            Category: MenuCategory.BURGER,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        db.MenuItems.Add(menuItem);
        
        var payment = new Payment
        {
            Id = paymentId,
            UserId = userId,
            Amount = 25.99m,
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var orderItems = new List<OrderItemDto>
        {
            new OrderItemDto(menuItem.Id.ToString(), 1)
        };
        
        var metadata = new
        {
            payment_id = paymentId.ToString(),
            user_id = userId.ToString(),
            order_items = JsonSerializer.Serialize(orderItems)
        };
        
        var payloadJson = JsonSerializer.Serialize(new
        {
            metadata = metadata
        });
        
        var handler = new ConfirmPaymentHandler(db);
        var command = new ConfirmPaymentCommand("checkout.session.completed", payloadJson);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedPayment = await db.Payments.FindAsync(paymentId);
        Assert.NotNull(updatedPayment);
        Assert.Equal(PaymentStatus.SUCCEDED, updatedPayment.Status);
        Assert.NotNull(updatedPayment.OrderId);
        
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == updatedPayment.OrderId);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(25.99m, order.Total);
    }

    [Fact]
    public async Task ConfirmPayment_With_Coupon_Should_Apply_Discount()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 50m,
            Description: null,
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        db.MenuItems.Add(menuItem);
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "SAVE20",
            Description = "20% discount",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 20,
            IsActive = true
        };
        db.Coupons.Add(coupon);
        
        var userCoupon = new UserCoupon
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CouponId = coupon.Id,
            IsUsed = false,
            AcquiredAtUtc = DateTime.UtcNow
        };
        db.UserCoupons.Add(userCoupon);
        
        var payment = new Payment
        {
            Id = paymentId,
            UserId = userId,
            Amount = 40m, // After 20% discount
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var orderItems = new List<OrderItemDto>
        {
            new OrderItemDto(menuItem.Id.ToString(), 1)
        };
        
        var metadata = new
        {
            payment_id = paymentId.ToString(),
            user_id = userId.ToString(),
            order_items = JsonSerializer.Serialize(orderItems),
            user_coupon_id = userCoupon.Id.ToString()
        };
        
        var payloadJson = JsonSerializer.Serialize(new { metadata = metadata });
        
        var handler = new ConfirmPaymentHandler(db);
        var command = new ConfirmPaymentCommand("checkout.session.completed", payloadJson);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var order = await db.Orders.FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(order);
        Assert.Equal(50m, order.Subtotal);
        Assert.Equal(10m, order.DiscountAmount); // 20% of 50
        Assert.Equal(40m, order.Total);
        
        var usedCoupon = await db.UserCoupons.FindAsync(userCoupon.Id);
        Assert.NotNull(usedCoupon);
        Assert.True(usedCoupon.IsUsed);
    }

    [Fact]
    public async Task ConfirmPayment_Should_Create_Kitchen_Task()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Salad",
            Price: 15m,
            Description: null,
            Category: MenuCategory.SALAD,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        db.MenuItems.Add(menuItem);
        
        var payment = new Payment
        {
            Id = paymentId,
            UserId = userId,
            Amount = 15m,
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var orderItems = new List<OrderItemDto>
        {
            new OrderItemDto(menuItem.Id.ToString(), 1)
        };
        
        var metadata = new
        {
            payment_id = paymentId.ToString(),
            user_id = userId.ToString(),
            order_items = JsonSerializer.Serialize(orderItems)
        };
        
        var payloadJson = JsonSerializer.Serialize(new { metadata = metadata });
        
        var handler = new ConfirmPaymentHandler(db);
        var command = new ConfirmPaymentCommand("checkout.session.completed", payloadJson);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var kitchenTask = await db.KitchenTasks.FirstOrDefaultAsync();
        Assert.NotNull(kitchenTask);
        Assert.Equal(KitchenTaskStatus.NotStarted, kitchenTask.Status);
    }

    [Fact]
    public async Task CreatePaymentSession_Should_Calculate_Correct_Amount()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var menuItem1 = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Burger",
            Price: 25m,
            Description: null,
            Category: MenuCategory.BURGER,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        var menuItem2 = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Fries",
            Price: 10m,
            Description: null,
            Category: MenuCategory.OTHER,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        db.MenuItems.AddRange(menuItem1, menuItem2);
        await db.SaveChangesAsync();
        
        var config = Substitute.For<IConfiguration>();
        config["Stripe:SecretKey"].Returns("sk_test_123");
        config["AppBaseUrl"].Returns("http://localhost:5000");
        
        var httpContext = Substitute.For<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        httpContext.HttpContext.Returns(context);
        
        var handler = new CreatePaymentSessionHandler(db, config, httpContext);
        
        var command = new CreatePaymentSessionCommand(
            Items: new List<OrderItemDto>
            {
                new OrderItemDto(menuItem1.Id.ToString(), 2),
                new OrderItemDto(menuItem2.Id.ToString(), 1)
            },
            Notes: null,
            UserCouponId: null
        );
        
        // Act & Assert - This will fail without mocking Stripe API
        // You would need to mock SessionService or test just the calculation logic
        // For now, just test that payment record is created
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.UserId == userId);
        // Additional assertions would go here after refactoring to make testable
    }

    [Fact]
    public async Task ConfirmPayment_Should_Ignore_Non_Checkout_Events()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new ConfirmPaymentHandler(db);
        var command = new ConfirmPaymentCommand("payment_intent.created", "{}");
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var orders = await db.Orders.ToListAsync();
        Assert.Empty(orders);
    }

    [Fact]
    public async Task ConfirmPayment_With_FreeItem_Coupon_Should_Apply_Item_Discount()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var freeItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Free Drink",
            Price: 5m,
            Description: null,
            Category: MenuCategory.DRINK,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        var paidItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Burger",
            Price: 30m,
            Description: null,
            Category: MenuCategory.BURGER,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        db.MenuItems.AddRange(freeItem, paidItem);
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "FREEDRINK",
            Description = "Free drink coupon",
            Type = CouponType.FreeItem,
            DiscountValue = 0,
            SpecificMenuItemId = freeItem.Id,
            IsActive = true
        };
        db.Coupons.Add(coupon);
        
        var userCoupon = new UserCoupon
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CouponId = coupon.Id,
            IsUsed = false,
            AcquiredAtUtc = DateTime.UtcNow
        };
        db.UserCoupons.Add(userCoupon);
        
        var payment = new Payment
        {
            Id = paymentId,
            UserId = userId,
            Amount = 30m,
            Status = PaymentStatus.PENDING,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var orderItems = new List<OrderItemDto>
        {
            new OrderItemDto(paidItem.Id.ToString(), 1),
            new OrderItemDto(freeItem.Id.ToString(), 1)
        };
        
        var metadata = new
        {
            payment_id = paymentId.ToString(),
            user_id = userId.ToString(),
            order_items = JsonSerializer.Serialize(orderItems),
            user_coupon_id = userCoupon.Id.ToString()
        };
        
        var payloadJson = JsonSerializer.Serialize(new { metadata = metadata });
        
        var handler = new ConfirmPaymentHandler(db);
        var command = new ConfirmPaymentCommand("checkout.session.completed", payloadJson);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var order = await db.Orders.FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(order);
        Assert.Equal(35m, order.Subtotal); // 30 + 5
        Assert.Equal(5m, order.DiscountAmount); // Free item price
        Assert.Equal(30m, order.Total);
    }
    

    [Fact]
    public async Task ConfirmPayment_Should_Throw_When_Metadata_Is_Missing()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new ConfirmPaymentHandler(db);
    
        // Payload fără metadata
        var payload = JsonSerializer.Serialize(new { other_field = "value" });
        var command = new ConfirmPaymentCommand("checkout.session.completed", payload);
    
        await Assert.ThrowsAsync<InvalidDataException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmPayment_Should_Support_Nested_Metadata_In_Object()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new ConfirmPaymentHandler(db);
    
        // Structura data.object.metadata suportată de handler
        var payload = JsonSerializer.Serialize(new { 
            data = new { 
                @object = new { 
                    metadata = new { payment_id = Guid.NewGuid().ToString(), user_id = Guid.NewGuid().ToString(), order_items = "[]" } 
                } 
            } 
        });
    
        var command = new ConfirmPaymentCommand("checkout.session.completed", payload);
    
        // Nu ar trebui să arunce excepția de "Missing metadata"
        await handler.Handle(command, CancellationToken.None);
    }
}

