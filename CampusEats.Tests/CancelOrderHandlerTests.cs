using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CampusEats.Tests;

public class CancelOrderHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_False_When_Order_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var httpContext = CreateHttpContext(Guid.NewGuid(), UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_User_Has_Invalid_Claims()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);

        var httpContext = CreateHttpContextWithInvalidClaim();
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_Order_Already_Cancelled()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id, OrderStatus.Cancelled);

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_Order_Already_Completed()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id, OrderStatus.Completed);

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_User_Not_Owner_And_Not_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var owner = await CreateUserAsync(db, "owner@test.com");
        var otherUser = await CreateUserAsync(db, "other@test.com");
        var order = await CreateOrderAsync(db, owner.Id);

        var httpContext = CreateHttpContext(otherUser.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_Should_Cancel_Order_When_User_Is_Owner()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var cancelledOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder!.Status);
    }

    [Fact]
    public async Task Handle_Should_Cancel_Order_When_User_Is_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var owner = await CreateUserAsync(db, "owner@test.com");
        var manager = await CreateUserAsync(db, "manager@test.com", UserRole.MANAGER);
        var order = await CreateOrderAsync(db, owner.Id);

        var httpContext = CreateHttpContext(manager.Id, UserRole.MANAGER);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var cancelledOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder!.Status);
    }

    [Fact]
    public async Task Handle_Should_Reverse_Loyalty_Points_When_Order_Had_Earned_Points()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Points = 150,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await db.LoyaltyAccounts.AddAsync(loyaltyAccount);
        
        var earnedTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned,
            Description = "Order reward",
            RelatedOrderId = order.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        await db.LoyaltyTransactions.AddAsync(earnedTransaction);
        await db.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedAccount = await db.LoyaltyAccounts.FindAsync(loyaltyAccount.Id);
        Assert.Equal(100, updatedAccount!.Points); // 150 - 50
        
        var reversalTransaction = await db.LoyaltyTransactions
            .FirstOrDefaultAsync(t => t.Type == LoyaltyTransactionType.Adjusted && t.RelatedOrderId == order.Id);
        Assert.NotNull(reversalTransaction);
        Assert.Equal(-50, reversalTransaction.PointsChange);
    }

    [Fact]
    public async Task Handle_Should_Deduct_Only_Available_Points_When_Account_Has_Less_Than_Earned()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Points = 30, // Has less than earned
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await db.LoyaltyAccounts.AddAsync(loyaltyAccount);
        
        var earnedTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 50, // Earned 50
            Type = LoyaltyTransactionType.Earned,
            Description = "Order reward",
            RelatedOrderId = order.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        await db.LoyaltyTransactions.AddAsync(earnedTransaction);
        await db.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedAccount = await db.LoyaltyAccounts.FindAsync(loyaltyAccount.Id);
        Assert.Equal(0, updatedAccount!.Points); // 30 - 30 (min of 30 and 50)
        
        var reversalTransaction = await db.LoyaltyTransactions
            .FirstOrDefaultAsync(t => t.Type == LoyaltyTransactionType.Adjusted && t.RelatedOrderId == order.Id);
        Assert.NotNull(reversalTransaction);
        Assert.Equal(-30, reversalTransaction.PointsChange);
    }

    [Fact]
    public async Task Handle_Should_Not_Reverse_Points_When_No_Loyalty_Account_Exists()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var cancelledOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder!.Status);
    }

    [Fact]
    public async Task Handle_Should_Cancel_Order_When_No_Earned_Points()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var order = await CreateOrderAsync(db, user.Id);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await db.LoyaltyAccounts.AddAsync(loyaltyAccount);
        await db.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new CancelOrderHandler(db, httpContext);
        var command = new CancelOrderCommand(order.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedAccount = await db.LoyaltyAccounts.FindAsync(loyaltyAccount.Id);
        Assert.Equal(100, updatedAccount!.Points); // No change
    }

    private static IHttpContextAccessor CreateHttpContext(Guid userId, UserRole role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(claimsPrincipal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContextAccessor;
    }

    private static IHttpContextAccessor CreateHttpContextWithInvalidClaim()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid"),
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(claimsPrincipal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContextAccessor;
    }

    private static async Task<User> CreateUserAsync(AppDbContext db, string email = "test@example.com", UserRole role = UserRole.STUDENT)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            PasswordHash = "hashedpassword",
            Role = role,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Order> CreateOrderAsync(AppDbContext db, Guid userId, OrderStatus status = OrderStatus.Pending)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Subtotal = 100m,
            DiscountAmount = 0m,
            Total = 100m,
            LoyaltyPointsAwarded = false
        };

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();
        return order;
    }
}
