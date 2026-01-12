using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Loyalty.GetLoyaltyTransactions;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class GetLoyaltyTransactionsHandlerTests
{
    [Fact]
    public async Task GetLoyaltyTransactions_Should_Return_Empty_List_When_User_Has_No_Transactions()
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
            Points = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);
        await db.SaveChangesAsync();

        var handler = new GetLoyaltyTransactionsHandler(db);
        var query = new GetLoyaltyTransactionsQuery(user.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLoyaltyTransactions_Should_Return_All_Transactions_For_User()
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
            Points = 150,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);

        var transaction1 = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned,
            Description = "Points earned from order",
            RelatedOrderId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        var transaction2 = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = -30,
            Type = LoyaltyTransactionType.Redeemed,
            Description = "Points redeemed for coupon",
            RelatedOrderId = null,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        db.LoyaltyTransactions.AddRange(transaction1, transaction2);
        await db.SaveChangesAsync();

        var handler = new GetLoyaltyTransactionsHandler(db);
        var query = new GetLoyaltyTransactionsQuery(user.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var dto1 = result.FirstOrDefault(t => t.Id == transaction1.Id);
        Assert.NotNull(dto1);
        Assert.Equal(50, dto1.PointsChange);
        Assert.Equal(LoyaltyTransactionType.Earned, dto1.Type);
        Assert.Equal("Points earned from order", dto1.Description);
        Assert.NotNull(dto1.RelatedOrderId);

        var dto2 = result.FirstOrDefault(t => t.Id == transaction2.Id);
        Assert.NotNull(dto2);
        Assert.Equal(-30, dto2.PointsChange);
        Assert.Equal(LoyaltyTransactionType.Redeemed, dto2.Type);
        Assert.Null(dto2.RelatedOrderId);
    }

    [Fact]
    public async Task GetLoyaltyTransactions_Should_Not_Return_Transactions_From_Other_Users()
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
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var loyalty2 = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Points = 200,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.AddRange(loyalty1, loyalty2);

        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyalty1.Id,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned,
            Description = "User 1 transaction",
            CreatedAtUtc = DateTime.UtcNow
        });

        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyalty2.Id,
            PointsChange = 75,
            Type = LoyaltyTransactionType.Earned,
            Description = "User 2 transaction",
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new GetLoyaltyTransactionsHandler(db);
        var query = new GetLoyaltyTransactionsQuery(user1.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("User 1 transaction", result[0].Description);
    }

    [Fact]
    public async Task GetLoyaltyTransactions_Should_Return_Transactions_Ordered_By_CreatedAtUtc_Descending()
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
            Points = 200,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(loyaltyAccount);

        var oldestTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned,
            Description = "Oldest",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };

        var middleTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 30,
            Type = LoyaltyTransactionType.Earned,
            Description = "Middle",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        var newestTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 20,
            Type = LoyaltyTransactionType.Adjusted,
            Description = "Newest",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.LoyaltyTransactions.AddRange(oldestTransaction, middleTransaction, newestTransaction);
        await db.SaveChangesAsync();

        var handler = new GetLoyaltyTransactionsHandler(db);
        var query = new GetLoyaltyTransactionsQuery(user.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Newest", result[0].Description);
        Assert.Equal("Middle", result[1].Description);
        Assert.Equal("Oldest", result[2].Description);
    }

    [Fact]
    public async Task GetLoyaltyTransactions_Should_Map_All_Transaction_Types_Correctly()
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

        var earnedTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned,
            Description = "Earned points",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        };

        var redeemedTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = -25,
            Type = LoyaltyTransactionType.Redeemed,
            Description = "Redeemed points",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        var adjustedTransaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = loyaltyAccount.Id,
            PointsChange = 10,
            Type = LoyaltyTransactionType.Adjusted,
            Description = "Adjusted points",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        db.LoyaltyTransactions.AddRange(earnedTransaction, redeemedTransaction, adjustedTransaction);
        await db.SaveChangesAsync();

        var handler = new GetLoyaltyTransactionsHandler(db);
        var query = new GetLoyaltyTransactionsQuery(user.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Type == LoyaltyTransactionType.Earned);
        Assert.Contains(result, t => t.Type == LoyaltyTransactionType.Redeemed);
        Assert.Contains(result, t => t.Type == LoyaltyTransactionType.Adjusted);
    }
}
