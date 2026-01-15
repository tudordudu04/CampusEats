using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Loyalty;
using CampusEats.Api.Features.Loyalty.GetLoyaltyTransactions;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.LoyaltyTests;

public class GetLoyaltyTransactionsHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Transactions_Ordered_By_Date_Descending()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetLoyaltyTransactionsHandler(db);
        var userId = Guid.NewGuid();

        // Setup User and Account
        var user = new User { Id = userId, Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 10 };
        
        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);

        // Setup Transactions
        var oldDate = DateTime.UtcNow.AddDays(-2);
        var newDate = DateTime.UtcNow.AddDays(-1);

        var t1 = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = account.Id,
            PointsChange = 5,
            Type = LoyaltyTransactionType.Earned,
            Description = "Old Transaction",
            CreatedAtUtc = oldDate
        };

        var t2 = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = account.Id,
            PointsChange = -5,
            Type = LoyaltyTransactionType.Redeemed,
            Description = "New Transaction",
            CreatedAtUtc = newDate
        };

        // Add unrelated transaction
        var otherAccount = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        db.LoyaltyAccounts.Add(otherAccount);
        db.LoyaltyTransactions.Add(new LoyaltyTransaction 
        { 
            LoyaltyAccountId = otherAccount.Id, 
            PointsChange = 10, 
            Description = "Other",
            Type = LoyaltyTransactionType.Earned 
        });

        db.LoyaltyTransactions.AddRange(t1, t2);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetLoyaltyTransactionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        // Check order (Descending)
        Assert.Equal(t2.Id, result[0].Id);
        Assert.Equal(t1.Id, result[1].Id);
        
        // Check mapping
        Assert.Equal("New Transaction", result[0].Description);
        Assert.Equal(LoyaltyTransactionType.Redeemed, result[0].Type);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_Account_Has_No_Transactions()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetLoyaltyTransactionsHandler(db);
        var userId = Guid.NewGuid();

        var user = new User { Id = userId, Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 0 };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetLoyaltyTransactionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_User_Has_No_Account()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetLoyaltyTransactionsHandler(db);
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new GetLoyaltyTransactionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

public class GetLoyaltyTransactionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetLoyaltyTransactionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetTransactions_Endpoints"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Get_Transactions_As_Student_Should_Return_Ok_And_List()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Transactor",
            Email = "trans@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };
        
        var account = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = 10
        };

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            PointsChange = 10,
            Type = LoyaltyTransactionType.Earned,
            Description = "Test Order",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        db.LoyaltyTransactions.Add(transaction);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/transactions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<LoyaltyTransactionDto>>();
        Assert.NotNull(dtos);
        Assert.Single(dtos);
        Assert.Equal(10, dtos[0].PointsChange);
        Assert.Equal("Test Order", dtos[0].Description);
    }

    [Fact]
    public async Task Get_Transactions_Should_Return_Ok_With_Empty_List_When_User_Has_No_Account()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "NewUser",
            Email = "new@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };
        
        db.Users.Add(user);
        // Do NOT add a LoyaltyAccount or Transactions
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/transactions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<LoyaltyTransactionDto>>();
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
    
    [Fact]
    public async Task Get_Transactions_Without_Token_Should_Return_Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/transactions");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
