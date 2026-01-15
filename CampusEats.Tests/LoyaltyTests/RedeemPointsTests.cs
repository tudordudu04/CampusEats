using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Loyalty.RedeemPoints;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CampusEats.Tests.LoyaltyTests;

public class RedeemPointsHandlerTests
{
    [Fact]
    public async Task Handle_Should_Deduct_Points_And_Create_Transaction_When_Successful()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new RedeemPointsHandler(db);
        var userId = Guid.NewGuid();

        var account = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var command = new RedeemPointsCommand(userId, 40, "Pizza Discount");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(60, result.RemainingPoints); // 100 - 40

        var dbAccount = await db.LoyaltyAccounts.FirstOrDefaultAsync(la => la.UserId == userId);
        Assert.Equal(60, dbAccount!.Points);

        var transaction = await db.LoyaltyTransactions.LastOrDefaultAsync();
        Assert.NotNull(transaction);
        Assert.Equal(-40, transaction.PointsChange);
        Assert.Equal(LoyaltyTransactionType.Redeemed, transaction.Type);
        Assert.Equal("Pizza Discount", transaction.Description);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Insufficient_Points()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new RedeemPointsHandler(db);
        var userId = Guid.NewGuid();

        var account = new LoyaltyAccount
        {
            UserId = userId,
            Points = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var command = new RedeemPointsCommand(userId, 50, "Big Discount");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Insufficient points", result.Message);
        
        // Verify no points deducted
        var dbAccount = await db.LoyaltyAccounts.FirstAsync(la => la.UserId == userId);
        Assert.Equal(10, dbAccount.Points);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Account_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new RedeemPointsHandler(db);
        var userId = Guid.NewGuid();

        var command = new RedeemPointsCommand(userId, 10, "Test");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Loyalty account not found", result.Message);
    }
}

public class RedeemPointsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    [Fact]
    public async Task Redeem_Should_Return_BadRequest_When_Points_Amount_Is_Invalid()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "NegativeUser", Email = "neg@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 100 };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Sending negative points (which would mathematically add points if not validated)
        request.Content = JsonContent.Create(new RedeemPointsRequest(-50, "Exploit Attempt"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Expecting BadRequest either from FluentValidation or Handler logic
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // specific check to ensure exploit didn't happen
        using var checkScope = _factory.Services.CreateScope();
        var checkDb = CreateDbContext(checkScope);
        var dbAccount = await checkDb.LoyaltyAccounts.FirstAsync(a => a.Id == account.Id);
        Assert.Equal(100, dbAccount.Points);
    }

    [Fact]
    public async Task Redeem_Should_Return_BadRequest_When_Description_Is_Missing()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "NoDescUser", Email = "nodesc@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 100 };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new RedeemPointsRequest(10, "")); // Empty description

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    public RedeemPointsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_RedeemPoints_Endpoints"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Redeem_Should_Return_Ok_When_Successful()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "User", Email = "redeem@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 100 };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new RedeemPointsRequest(30, "Burger Discount"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RedeemPointsResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(70, result.RemainingPoints); // 100 - 30
    }

    [Fact]
    public async Task Redeem_Should_Return_BadRequest_When_Insufficient_Points()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "User2", Email = "redeem2@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), UserId = userId, Points = 5 };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new RedeemPointsRequest(10, "Expensive Item"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RedeemPointsResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Insufficient points", result.Message);
    }

    [Fact]
    public async Task Redeem_Should_Return_BadRequest_When_Account_Not_Found()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "NoAccount", Email = "noacc@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        
        db.Users.Add(user);
        // deliberately not creating a LoyaltyAccount
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new RedeemPointsRequest(10, "Test"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RedeemPointsResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Loyalty account not found", result.Message);
    }

    [Fact]
    public async Task Redeem_Should_Return_Unauthorized_When_No_Token()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/loyalty/redeem");
        request.Content = JsonContent.Create(new RedeemPointsRequest(10, "Test"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Validation Test (Optional - assumes FluentValidation is wired up in pipeline)
    [Fact]
    public async Task Validator_Should_Validate_Request()
    {
        var validator = new RedeemPointsValidator();
        
        var badRequest = new RedeemPointsCommand(Guid.NewGuid(), -5, "");
        var result = await validator.ValidateAsync(badRequest);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Points");
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}
