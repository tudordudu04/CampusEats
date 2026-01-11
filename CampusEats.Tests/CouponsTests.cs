using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Coupons.CreateCoupon;
using CampusEats.Api.Features.Coupons.PurchaseCoupon;
using CampusEats.Api.Features.Coupons.GetAvailableCoupons;
using CampusEats.Api.Features.Coupons.GetUserCoupons;
using CampusEats.Api.Features.Coupons.DeleteCoupon;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class CouponsTests
{
    [Fact]
    public async Task CreateCoupon_Should_Create_Percentage_Discount_Coupon()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new CreateCouponHandler(db);
        
        var command = new CreateCouponCommand(
            Name: "20% Off",
            Description: "Get 20% off your order",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 20,
            PointsCost: 100,
            SpecificMenuItemId: null,
            MinimumOrderAmount: 50m,
            ExpiresAtUtc: null
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var coupon = await db.Coupons.FindAsync(result.CouponId);
        Assert.NotNull(coupon);
        Assert.Equal("20% Off", coupon.Name);
        Assert.Equal(CouponType.PercentageDiscount, coupon.Type);
        Assert.Equal(20, coupon.DiscountValue);
        Assert.Equal(100, coupon.PointsCost);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public async Task CreateCoupon_Should_Create_Fixed_Amount_Coupon()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new CreateCouponHandler(db);
        
        var command = new CreateCouponCommand(
            Name: "10 RON Off",
            Description: "Get 10 RON discount",
            Type: CouponType.FixedAmountDiscount,
            DiscountValue: 10,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var coupon = await db.Coupons.FindAsync(result.CouponId);
        Assert.NotNull(coupon);
        Assert.Equal(CouponType.FixedAmountDiscount, coupon.Type);
        Assert.Equal(10, coupon.DiscountValue);
    }

    [Fact]
    public async Task CreateCoupon_Should_Create_FreeItem_Coupon()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Free Coffee",
            Price: 8m,
            Description: null,
            Category: MenuCategory.DRINK,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        db.MenuItems.Add(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new CreateCouponHandler(db);
        
        var command = new CreateCouponCommand(
            Name: "Free Coffee Coupon",
            Description: "Get a free coffee",
            Type: CouponType.FreeItem,
            DiscountValue: 0,
            PointsCost: 75,
            SpecificMenuItemId: menuItem.Id,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var coupon = await db.Coupons.FindAsync(result.CouponId);
        Assert.NotNull(coupon);
        Assert.Equal(CouponType.FreeItem, coupon.Type);
        Assert.Equal(menuItem.Id, coupon.SpecificMenuItemId);
    }

    [Fact]
    public async Task CreateCoupon_Should_Fail_When_MenuItem_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new CreateCouponHandler(db);
        
        var command = new CreateCouponCommand(
            Name: "Invalid Coupon",
            Description: "Invalid menu item reference",
            Type: CouponType.FreeItem,
            DiscountValue: 0,
            PointsCost: 50,
            SpecificMenuItemId: Guid.NewGuid(),
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Menu item not found", result.Message);
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Deduct_Points_And_Create_UserCoupon()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Test Coupon",
            Description = "Test",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 15,
            PointsCost = 50,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(50, result.RemainingPoints); // 100 - 50
        
        var updatedAccount = await db.LoyaltyAccounts.FirstOrDefaultAsync(la => la.UserId == userId);
        Assert.NotNull(updatedAccount);
        Assert.Equal(50, updatedAccount.Points);
        
        var userCoupon = await db.UserCoupons.FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id);
        Assert.NotNull(userCoupon);
        Assert.False(userCoupon.IsUsed);
        
        var transaction = await db.LoyaltyTransactions.FirstOrDefaultAsync(lt => lt.LoyaltyAccountId == loyaltyAccount.Id);
        Assert.NotNull(transaction);
        Assert.Equal(-50, transaction.PointsChange);
        Assert.Equal(LoyaltyTransactionType.Redeemed, transaction.Type);
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Fail_With_Insufficient_Points()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Expensive Coupon",
            Description = "Test description",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 25,
            PointsCost = 200,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = userId,
            Points = 50, // Not enough
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient points", result.Message);
        
        var updatedAccount = await db.LoyaltyAccounts.FirstOrDefaultAsync(la => la.UserId == userId);
        Assert.Equal(50, updatedAccount.Points); // Unchanged
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Fail_When_Coupon_Expired()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Expired Coupon",
            Description = "Expired test coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 10,
            PointsCost = 30,
            IsActive = true,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };
        db.Coupons.Add(coupon);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Coupon has expired", result.Message);
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Fail_When_Coupon_Inactive()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Coupon",            Description = "Inactive test coupon",            Type = CouponType.PercentageDiscount,
            DiscountValue = 10,
            PointsCost = 30,
            IsActive = false, // Inactive
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Coupon is not available", result.Message);
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Fail_When_Loyalty_Account_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Test Coupon",            Description = "Test coupon description",            Type = CouponType.PercentageDiscount,
            DiscountValue = 10,
            PointsCost = 30,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Loyalty account not found", result.Message);
    }

    [Fact]
    public async Task PurchaseCoupon_Should_Set_Expiration_From_Coupon()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var expirationDate = DateTime.UtcNow.AddDays(30);
        
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Limited Time Coupon",
            Description = "Limited time test coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 15,
            PointsCost = 40,
            IsActive = true,
            ExpiresAtUtc = expirationDate,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        
        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();
        
        var handler = new PurchaseCouponHandler(db);
        var command = new PurchaseCouponCommand(userId, coupon.Id);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var userCoupon = await db.UserCoupons.FirstOrDefaultAsync(uc => uc.UserId == userId);
        Assert.NotNull(userCoupon);
        Assert.Equal(expirationDate, userCoupon.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetAvailableCoupons_Should_Return_Active_Coupons()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var activeCoupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Active Coupon",
            Description = "Active test coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 10,
            PointsCost = 50,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        
        var inactiveCoupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Coupon",
            Description = "Inactive test coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 20,
            PointsCost = 100,
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        
        db.Coupons.AddRange(activeCoupon, inactiveCoupon);
        await db.SaveChangesAsync();
        
        var handler = new GetAvailableCouponsHandler(db);
        var query = new GetAvailableCouponsQuery(userId);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("Active Coupon", result[0].Name);
    }

    [Fact]
    public async Task GetUserCoupons_Should_Return_Purchased_Unused_Coupons()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        
        var coupon = new Coupon
        {
            Id = couponId,
            Name = "My Coupon",
            Description = "User's purchased coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 15,
            PointsCost = 100,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        
        var userCoupon = new UserCoupon
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CouponId = couponId,
            IsUsed = false,
            AcquiredAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };
        db.UserCoupons.Add(userCoupon);
        await db.SaveChangesAsync();
        
        var handler = new GetUserCouponsHandler(db);
        var query = new GetUserCouponsQuery(userId);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("My Coupon", result[0].CouponName);
        Assert.False(result[0].IsUsed);
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Valid Coupon",
            Description: "A valid coupon description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 15m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: 20m,
            ExpiresAtUtc: DateTime.UtcNow.AddDays(30)
        );

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "",
            Description: "Description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name is required");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_Name_Exceeds_Maximum_Length()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: new string('A', 101),
            Description: "Description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name must not exceed 100 characters");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_Description_Is_Empty()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon Name",
            Description: "",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Description is required");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_Description_Exceeds_Maximum_Length()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon Name",
            Description: new string('B', 501),
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Description must not exceed 500 characters");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_DiscountValue_Is_Negative()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon",
            Description: "Description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: -5m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Discount value must be greater than or equal to 0");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_DiscountValue_Is_Zero_For_NonFreeItem()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon",
            Description: "Description",
            Type: CouponType.FixedAmountDiscount,
            DiscountValue: 0m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Discount value must be greater than 0 for percentage and fixed discounts");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Pass_When_DiscountValue_Is_Zero_For_FreeItem()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Free Item Coupon",
            Description: "Get a free item",
            Type: CouponType.FreeItem,
            DiscountValue: 0m,
            PointsCost: 100,
            SpecificMenuItemId: Guid.NewGuid(),
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_PointsCost_Is_Zero()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon",
            Description: "Description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 0,
            SpecificMenuItemId: null,
            MinimumOrderAmount: null,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Points cost must be greater than 0");
    }

    [Fact]
    public async Task CreateCouponValidator_Should_Fail_When_MinimumOrderAmount_Is_Negative()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand(
            Name: "Coupon",
            Description: "Description",
            Type: CouponType.PercentageDiscount,
            DiscountValue: 10m,
            PointsCost: 50,
            SpecificMenuItemId: null,
            MinimumOrderAmount: -10m,
            ExpiresAtUtc: null
        );

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Minimum order amount must be greater than or equal to 0");
    }
}