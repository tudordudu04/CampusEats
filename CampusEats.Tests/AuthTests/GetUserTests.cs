using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth;
using CampusEats.Api.Features.Auth.GetUser;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.AuthTests;

public class GetUserTests
{
    [Fact]
    public async Task Handle_Should_Return_UserDto_When_User_Exists()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetUserHandler(db);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com",
            Role = UserRole.WORKER,
            PasswordHash = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var query = new GetUserQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("WORKER", result.Role);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_User_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetUserHandler(db);

        var query = new GetUserQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}

public class GetUserEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetUserEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetUser"));
                
                // Important: Do NOT mock IJwtTokenService here if we want the server to validate tokens correctly.
                // The real JwtTokenService will use the real JwtOptions (likely from appsettings.Development.json).
                // If those keys are consistent, the token generated below will be accepted.
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Get_Me_Should_Return_Ok_With_User_Details()
    {
        var client = _factory.CreateClient();
        
        // 1. Create a scope to resolve services from the same factory that runs the server
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 2. Seed User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Integration User",
            Email = "me@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 3. Generate a VALID token using the server's own logic
        // This ensures the signing key matches what the authentication middleware expects
        var token = jwtService.GenerateAccessToken(user);

        // 4. Attach token to request
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 5. Act
        var response = await client.SendAsync(request);

        // 6. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var dto = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(dto);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("Integration User", dto.Name);
    }

    [Fact]
    public async Task Get_Me_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
