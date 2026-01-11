using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Features.Orders.GetOrders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace CampusEats.Tests;

public class GetOrdersHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Null_When_User_Is_Null()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var httpContext = CreateHttpContextWithoutUser();
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_UserId_Claim_Is_Invalid()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var httpContext = CreateHttpContextWithInvalidClaim();
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Order_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(Guid.NewGuid()); // Non-existent order

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_User_Is_Not_Owner_And_Not_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var owner = await CreateUserAsync(db, "owner@test.com");
        var otherUser = await CreateUserAsync(db, "other@test.com");
        var menuItem = await CreateMenuItemAsync(db);
        var order = await CreateOrderAsync(db, owner.Id, menuItem.Id);
        
        var httpContext = CreateHttpContext(otherUser.Id, UserRole.STUDENT);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Order_When_User_Is_Owner()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var menuItem = await CreateMenuItemAsync(db);
        var order = await CreateOrderAsync(db, user.Id, menuItem.Id);
        
        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(order.Status, result.Status);
        Assert.Equal(order.Total, result.Total);
        Assert.Single(result.Items);
        Assert.Equal(menuItem.Name, result.Items[0].MenuItemName);
    }

    [Fact]
    public async Task Handle_Should_Return_Order_When_User_Is_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var owner = await CreateUserAsync(db, "owner@test.com");
        var manager = await CreateUserAsync(db, "manager@test.com", UserRole.MANAGER);
        var menuItem = await CreateMenuItemAsync(db);
        var order = await CreateOrderAsync(db, owner.Id, menuItem.Id);
        
        var httpContext = CreateHttpContext(manager.Id, UserRole.MANAGER);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(owner.Id, result.UserId);
    }

    [Fact]
    public async Task Handle_Should_Return_Order_When_User_Is_Worker()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var owner = await CreateUserAsync(db, "owner@test.com");
        var worker = await CreateUserAsync(db, "worker@test.com", UserRole.WORKER);
        var menuItem = await CreateMenuItemAsync(db);
        var order = await CreateOrderAsync(db, owner.Id, menuItem.Id);
        
        var httpContext = CreateHttpContext(worker.Id, UserRole.WORKER);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(owner.Id, result.UserId);
    }

    [Fact]
    public async Task Handle_Should_Return_Order_With_Multiple_Items()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var menuItem1 = await CreateMenuItemAsync(db, "Pizza");
        var menuItem2 = await CreateMenuItemAsync(db, "Burger");
        var menuItem3 = await CreateMenuItemAsync(db, "Salad");
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Subtotal = 100m,
            DiscountAmount = 0m,
            Total = 100m,
            LoyaltyPointsAwarded = false
        };
        await db.Orders.AddAsync(order);

        var orderItems = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), OrderId = order.Id, MenuItemId = menuItem1.Id, Quantity = 2, UnitPrice = 20m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, MenuItemId = menuItem2.Id, Quantity = 1, UnitPrice = 30m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, MenuItemId = menuItem3.Id, Quantity = 3, UnitPrice = 10m }
        };
        await db.OrderItems.AddRangeAsync(orderItems);
        await db.SaveChangesAsync();
        
        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, i => i.MenuItemName == "Pizza" && i.Quantity == 2);
        Assert.Contains(result.Items, i => i.MenuItemName == "Burger" && i.Quantity == 1);
        Assert.Contains(result.Items, i => i.MenuItemName == "Salad" && i.Quantity == 3);
    }

    [Fact]
    public async Task Handle_Should_Handle_Missing_MenuItem_Names_Gracefully()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var user = await CreateUserAsync(db);
        var menuItem = await CreateMenuItemAsync(db, "ExistingItem");
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Subtotal = 50m,
            DiscountAmount = 0m,
            Total = 50m,
            LoyaltyPointsAwarded = false
        };
        await db.Orders.AddAsync(order);

        var deletedMenuItemId = Guid.NewGuid(); // MenuItem that doesn't exist
        var orderItems = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), OrderId = order.Id, MenuItemId = menuItem.Id, Quantity = 1, UnitPrice = 25m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, MenuItemId = deletedMenuItemId, Quantity = 1, UnitPrice = 25m }
        };
        await db.OrderItems.AddRangeAsync(orderItems);
        await db.SaveChangesAsync();
        
        var httpContext = CreateHttpContext(user.Id, UserRole.STUDENT);
        var handler = new GetOrdersHandler(db, httpContext);
        
        var query = new GetOrdersQuerry(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, i => i.MenuItemName == "ExistingItem");
        Assert.Contains(result.Items, i => i.MenuItemName == null); // Deleted item
    }

    // Helper methods
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

    private static IHttpContextAccessor CreateHttpContextWithoutUser()
    {
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns((ClaimsPrincipal)null!);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContextAccessor;
    }

    private static IHttpContextAccessor CreateHttpContextWithInvalidClaim()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
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

    private static async Task<MenuItem> CreateMenuItemAsync(AppDbContext db, string name = "Test Item")
    {
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: name,
            Price: 10.00m,
            Description: null,
            Category: MenuCategory.BURGER,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );

        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        return menuItem;
    }

    private static async Task<Order> CreateOrderAsync(AppDbContext db, Guid userId, Guid menuItemId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Subtotal = 50m,
            DiscountAmount = 0m,
            Total = 50m,
            LoyaltyPointsAwarded = false
        };

        await db.Orders.AddAsync(order);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            MenuItemId = menuItemId,
            Quantity = 1,
            UnitPrice = 50m
        };

        await db.OrderItems.AddAsync(orderItem);
        await db.SaveChangesAsync();
        
        return order;
    }
}
