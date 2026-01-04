using Xunit;
using Microsoft.EntityFrameworkCore;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Coupons.PurchaseCoupon;
using CampusEats.Api.Features.Auth.UpdateUserProfile;
using CampusEats.Api.Features.Auth.DeleteUser;
using NSubstitute;
using Microsoft.AspNetCore.Http;

namespace CampusEats.Tests;

public class CommandsTests
{
    [Fact]
    public async Task PurchaseCoupon_Should_Fail_If_Insufficient_Points()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var couponId = Guid.NewGuid();

        db.LoyaltyAccounts.Add(new LoyaltyAccount 
        { 
            UserId = userId, 
            Points = 10,
            UpdatedAtUtc = DateTime.UtcNow 
        });
        
        db.Coupons.Add(new Coupon 
        { 
            Id = couponId, 
            PointsCost = 50, 
            IsActive = true, 
            Name = "Discount", 
            Description = "50 points off" 
        });
        await db.SaveChangesAsync();

        // FIX: PurchaseCouponHandler primește DOAR AppDbContext
        var handler = new PurchaseCouponHandler(db);
        
        // FIX: PurchaseCouponCommand necesită UserId ȘI CouponId
        var command = new PurchaseCouponCommand(userId, couponId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient points", result.Message);
    }

    [Fact]
    public async Task UpdateUserProfile_Should_Modify_User_Data()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var user = new User 
        { 
            Id = userId, 
            Name = "Old Name", 
            Email = "old@test.com", 
            PasswordHash = "hash", 
            Role = UserRole.STUDENT 
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var httpContextAccessor = TestDbHelper.SetupUserContext(userId);
        var handler = new UpdateUserProfileHandler(db, httpContextAccessor);
    
        // Notă: Am eliminat Email pentru că nu există în definiția record-ului tău
        var command = new UpdateUserProfileCommand 
        { 
            Name = "New Name", 
            AddressCity = "City", 
            AddressStreet = "Street", 
            AddressNumber = "123", 
            AddressDetails = "Details" 
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await db.Users.FindAsync(userId);
        Assert.Equal("New Name", updatedUser!.Name);
        Assert.Equal("City", updatedUser.AddressCity);
    }

    [Fact]
    public async Task DeleteUser_Should_Remove_User_And_Tokens()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        db.Users.Add(new User 
        { 
            Id = userId, 
            Name = "To Delete", 
            Email = "del@test.com", 
            PasswordHash = "hash", 
            Role = UserRole.STUDENT 
        });
        
        db.RefreshTokens.Add(new RefreshToken 
        { 
            UserId = userId, 
            TokenHash = "token_hash", 
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1) 
        });
        await db.SaveChangesAsync();

        // FIX: DeleteUserHandler necesită AppDbContext ȘI IHttpContextAccessor (Manager)
        var httpContextAccessor = TestDbHelper.SetupUserContext(managerId, "MANAGER");
        var handler = new DeleteUserHandler(db, httpContextAccessor);
        
        var command = new DeleteUserCommand(userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deletedUser = await db.Users.FindAsync(userId);
        Assert.Null(deletedUser);
        
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        Assert.Empty(tokens);
    }
}