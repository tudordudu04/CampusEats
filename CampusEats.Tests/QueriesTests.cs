using Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Microsoft.AspNetCore.Http;
using CampusEats.Api.Enums;
using CampusEats.Api.Domain;
using CampusEats.Api.Features.Auth.GetUser;
using CampusEats.Api.Features.Auth.GetAllUsers;
using CampusEats.Api.Features.Menu.GetMenuItem;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using CampusEats.Api.Features.Inventory.GetStockByName;
using CampusEats.Api.Features.Inventory.GetAllIngredientsInStock; 
using CampusEats.Api.Features.Coupons.GetAvailableCoupons;
using CampusEats.Api.Features.Loyalty.GetLoyaltyAccount;
using CampusEats.Api.Features.Orders.GetAllOrders;
using CampusEats.Api.Features.Orders.GetOrders; 

namespace CampusEats.Tests;

public class QueriesTests
{
    // --- AUTH QUERIES ---

    [Fact]
    public async Task GetUser_Should_Return_Null_When_User_Not_Exists()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetUserHandler(db);
        
        // GetUserQuery primește Guid Id
        var result = await handler.Handle(new GetUserQuery(Guid.NewGuid()), CancellationToken.None);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllUsers_Should_Return_Ok_With_List()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var http = TestDbHelper.SetupUserContext(userId, "MANAGER");
        var handler = new GetAllUsersHandler(db, http);
        
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);
        
        // GetAllUsersHandler returnează IResult (Results.Ok)
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<CampusEats.Api.Features.Auth.UserDto>>>(result);
        Assert.Empty(okResult.Value);
    }

    // --- MENU QUERIES ---

    [Fact]
    public async Task GetAllMenuItems_Should_Return_All_Items()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        db.MenuItems.Add(new MenuItem(Guid.NewGuid(), "Pizza", 20, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new GetAllMenuItemsHandler(db);
        // GetAllMenuItemsQuery nu are parametri în constructor
        var result = await handler.Handle(new GetAllMenuItemsQuery(), CancellationToken.None);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetMenuItem_Should_Return_Dto_When_Exists()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Coke", 5, "Cold", MenuCategory.DRINK, null, []));
        await db.SaveChangesAsync();

        var handler = new GetMenuItemHandler(db);
        var result = await handler.Handle(new GetMenuItemQuery(id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Coke", result.Name);
    }

    // --- INVENTORY QUERIES ---

    [Fact]
    public async Task GetStockByName_Should_Return_NotFound_When_No_Match()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var validator = Substitute.For<FluentValidation.IValidator<GetStockByNameCommand>>();
        validator.ValidateAsync(Arg.Any<GetStockByNameCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new GetStockByNameHandler(db, validator);
        // GetStockByNameCommand primește string Name
        var result = await handler.Handle(new GetStockByNameCommand("Tomato"), CancellationToken.None);

        // Returnează Results.NotFound dacă nu există
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>(result);
    }

    // --- LOYALTY QUERIES ---

    [Fact]
    public async Task GetLoyaltyAccount_Should_Return_Null_When_No_Account()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetLoyaltyAccountHandler(db);
    
        // Act
        // Generăm un Guid care sigur nu există în baza de date goală
        var result = await handler.Handle(new GetLoyaltyAccountQuery(Guid.NewGuid()), CancellationToken.None);
    
        // Assert
        // Handler-ul returnează LoyaltyAccountDto? care este null dacă nu este găsit
        Assert.Null(result);
    }

    // --- COUPON QUERIES ---

    [Fact]
    public async Task GetAvailableCoupons_Should_Ignore_Expired_Coupons()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        db.Coupons.Add(new Coupon { 
            Name = "Old", 
            Description = "Exp", 
            IsActive = true, 
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) 
        });
        await db.SaveChangesAsync();

        var handler = new GetAvailableCouponsHandler(db);
        // GetAvailableCouponsQuery primește Guid UserId
        var result = await handler.Handle(new GetAvailableCouponsQuery(Guid.NewGuid()), CancellationToken.None);
        
        Assert.Empty(result);
    }

    // --- ORDER QUERIES ---

    [Fact]
    public async Task GetAllOrders_Should_Return_Orders_For_User()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        db.Orders.Add(new Order { 
            Id = Guid.NewGuid(), 
            UserId = userId, 
            Total = 50, 
            CreatedAt = DateTime.UtcNow 
        });
        await db.SaveChangesAsync();

        // GetAllOrdersHandler cere (AppDbContext, IHttpContextAccessor)
        var http = TestDbHelper.SetupUserContext(userId);
        var handler = new GetAllOrdersHandler(db, http);
        
        // GetAllOrdersQuery are parametrul opțional bool All
        var result = await handler.Handle(new GetAllOrdersQuery(false), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(50, result[0].Total);
    }
}