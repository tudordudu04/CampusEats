using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Loyalty;
using CampusEats.Api.Features.Loyalty.GetLoyaltyAccount;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.LoyaltyTests;

public class GetLoyaltyAccountHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Dto_When_Account_Exists()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();

        var userId = Guid.NewGuid();
        var handler = new GetLoyaltyAccountHandler(db);

        var user = new User
        {
            Id = userId,
            Name = "Loyal User",
            Email = "loyal@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };

        var account = new LoyaltyAccount
        {
            UserId = userId,
            Points = 100,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetLoyaltyAccountQuery(userId), CancellationToken.None);

        // Assert
        // The handler returns LoyaltyAccountDto?, not IResult.
        Assert.NotNull(result);
        Assert.Equal(100, result.Points);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Account_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var handler = new GetLoyaltyAccountHandler(db);

        // Act
        var result = await handler.Handle(new GetLoyaltyAccountQuery(userId), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}

public class GetLoyaltyAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    [Fact]
    public async Task Get_Loyalty_As_Worker_Should_Return_Ok_And_Data()
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
            Name = "WorkerUser", 
            Email = "worker@test.com", 
            Role = UserRole.WORKER, // Verify Worker role access
            PasswordHash = "x" 
        };
        
        var account = new LoyaltyAccount 
        { 
            Id = Guid.NewGuid(), 
            UserId = userId, 
            Points = 500 
        };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/account");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LoyaltyAccountDto>();
        Assert.NotNull(dto);
        Assert.Equal(500, dto.Points);
    }
    
    public GetLoyaltyAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetLoyaltyAccount_Final"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Get_Loyalty_As_Student_Should_Return_Ok_And_Data()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed User and Account
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Student",
            Email = "student@loyalty.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };

        var account = new LoyaltyAccount
        {
            UserId = userId,
            Points = 50,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.LoyaltyAccounts.Add(account);
        await db.SaveChangesAsync();

        // 2. Generate Token
        var token = jwtService.GenerateAccessToken(user);

        // 3. Request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/account");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 4. Act
        var response = await client.SendAsync(request);

        // 5. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LoyaltyAccountDto>();
        Assert.NotNull(dto);
        Assert.Equal(50, dto.Points);
        Assert.Equal(userId, dto.UserId);
    }

    [Fact]
    public async Task Get_Loyalty_Without_Account_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed User ONLY (No LoyaltyAccount)
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "New Student",
            Email = "new@loyalty.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);

        // 2. Request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/account");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 3. Act
        var response = await client.SendAsync(request);

        // 4. Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Loyalty_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/account");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
