using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Coupons.DeleteCoupon;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class DeleteCouponHandlerTests
{
    [Fact]
    public async Task DeleteCoupon_Should_Return_Error_When_Coupon_Does_Not_Exist()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteCouponHandler(db);
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteCouponCommand(nonExistentId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Coupon not found", result.Message);
    }

    [Fact]
    public async Task DeleteCoupon_Should_Delete_Coupon_With_No_Purchased_Users()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Unused Coupon",
            Description = "Never purchased",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 10m,
            PointsCost = 50,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();

        var handler = new DeleteCouponHandler(db);
        var command = new DeleteCouponCommand(coupon.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Refunded 0 users", result.Message);
        var deletedCoupon = await db.Coupons.FindAsync(coupon.Id);
        Assert.Null(deletedCoupon);
    }

    [Fact]
    public async Task DeleteCoupon_Should_Refund_Points_To_User_Who_Purchased_Coupon()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);

        var loyaltyAccount = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Premium Coupon",
            Description = "Expensive coupon",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 20m,
            PointsCost = 150,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);

        var userCoupon = new UserCoupon
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CouponId = coupon.Id,
            AcquiredAtUtc = DateTime.UtcNow,
            IsUsed = false
        };
        db.UserCoupons.Add(userCoupon);
        await db.SaveChangesAsync();

        var handler = new DeleteCouponHandler(db);
        var command = new DeleteCouponCommand(coupon.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Refunded 1 users", result.Message);

        var updatedLoyalty = await db.LoyaltyAccounts.FindAsync(loyaltyAccount.Id);
        Assert.NotNull(updatedLoyalty);
        Assert.Equal(250, updatedLoyalty.Points);

        var transaction = await db.LoyaltyTransactions
            .FirstOrDefaultAsync(t => t.LoyaltyAccountId == loyaltyAccount.Id);
        Assert.NotNull(transaction);
        Assert.Equal(150, transaction.PointsChange);
        Assert.Equal(LoyaltyTransactionType.Adjusted, transaction.Type);
        Assert.Contains("Premium Coupon", transaction.Description);

        var deletedCoupon = await db.Coupons.FindAsync(coupon.Id);
        Assert.Null(deletedCoupon);

        var deletedUserCoupon = await db.UserCoupons.FindAsync(userCoupon.Id);
        Assert.Null(deletedUserCoupon);
    }

    [Fact]
    public async Task DeleteCoupon_Should_Refund_Points_To_Multiple_Users()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User 1",
            Email = "user1@test.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User 2",
            Email = "user2@test.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Users.AddRange(user1, user2);

        var loyalty1 = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Points = 50,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var loyalty2 = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Points = 75,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.AddRange(loyalty1, loyalty2);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Popular Coupon",
            Description = "Many users bought this",
            Type = CouponType.FixedAmountDiscount,
            DiscountValue = 10m,
            PointsCost = 80,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);

        db.UserCoupons.AddRange(
            new UserCoupon
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                CouponId = coupon.Id,
                AcquiredAtUtc = DateTime.UtcNow,
                IsUsed = false
            },
            new UserCoupon
            {
                Id = Guid.NewGuid(),
                UserId = user2.Id,
                CouponId = coupon.Id,
                AcquiredAtUtc = DateTime.UtcNow,
                IsUsed = false
            }
        );
        await db.SaveChangesAsync();

        var handler = new DeleteCouponHandler(db);
        var command = new DeleteCouponCommand(coupon.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Refunded 2 users", result.Message);

        var updatedLoyalty1 = await db.LoyaltyAccounts.FindAsync(loyalty1.Id);
        Assert.Equal(130, updatedLoyalty1!.Points);

        var updatedLoyalty2 = await db.LoyaltyAccounts.FindAsync(loyalty2.Id);
        Assert.Equal(155, updatedLoyalty2!.Points);

        var transactions = await db.LoyaltyTransactions.ToListAsync();
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(80, t.PointsChange));
    }

    [Fact]
    public async Task DeleteCoupon_Should_Handle_User_Without_Loyalty_Account()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "User Without Loyalty",
            Email = "noloyalty@test.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Name = "Test Coupon",
            Description = "Test",
            Type = CouponType.PercentageDiscount,
            DiscountValue = 5m,
            PointsCost = 25,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);

        var userCoupon = new UserCoupon
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CouponId = coupon.Id,
            AcquiredAtUtc = DateTime.UtcNow,
            IsUsed = false
        };
        db.UserCoupons.Add(userCoupon);
        await db.SaveChangesAsync();

        var handler = new DeleteCouponHandler(db);
        var command = new DeleteCouponCommand(coupon.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Refunded 1 users", result.Message);

        var transactions = await db.LoyaltyTransactions.ToListAsync();
        Assert.Empty(transactions);

        var deletedCoupon = await db.Coupons.FindAsync(coupon.Id);
        Assert.Null(deletedCoupon);
    }
}
