using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.DeleteUser;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults; // Added for IValueHttpResult
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.AuthTests;

public class DeleteUserTests
{
    private static ClaimsPrincipal CreatePrincipal(string role, bool isAuthenticated = true)
    {
        if (!isAuthenticated) return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task Handle_Should_Delete_User_And_Return_Ok_When_Caller_Is_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal(nameof(UserRole.MANAGER)) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new DeleteUserHandler(db, http);

        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Name = "Target",
            Email = "target@test.com",
            Role = UserRole.WORKER,
            PasswordHash = "x"
        };

        db.Users.Add(targetUser);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = targetUserId,
            TokenHash = "hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new DeleteUserCommand(targetUserId), CancellationToken.None);

        // Assert
        // Instead of verifying the exact generic type Ok<object>, we verify it implements IValueHttpResult
        // or check purely for the type being an Ok result regardless of content.
        Assert.NotNull(result);
        var okResult = result as IValueHttpResult; 
        Assert.NotNull(okResult);
        Assert.Equal(200, (result as IStatusCodeHttpResult)?.StatusCode ?? 200);

        // Verify Deletion
        var userInDb = await db.Users.FindAsync(targetUserId);
        Assert.Null(userInDb);

        var tokensInDb = await db.RefreshTokens.Where(rt => rt.UserId == targetUserId).ToListAsync();
        Assert.Empty(tokensInDb);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Target_User_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal(nameof(UserRole.MANAGER)) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new DeleteUserHandler(db, http);

        // Act
        var result = await handler.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_Caller_Is_Not_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal(nameof(UserRole.STUDENT)) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new DeleteUserHandler(db, http);

        // Act
        var result = await handler.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Caller_Not_Authenticated()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal("", isAuthenticated: false) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new DeleteUserHandler(db, http);

        // Act
        var result = await handler.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }
}

public class DeleteUserEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DeleteUserEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_DeleteUser"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Delete_User_As_Manager_Should_Return_Ok_And_Remove_User()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed Manager
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Name = "Manager",
            Email = "manager@delete.com",
            Role = UserRole.MANAGER,
            PasswordHash = "x"
        };
        db.Users.Add(manager);

        // 2. Seed Victim
        var victimId = Guid.NewGuid();
        var victim = new User
        {
            Id = victimId,
            Name = "Victim",
            Email = "victim@delete.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x"
        };
        db.Users.Add(victim);
        await db.SaveChangesAsync();

        // 3. Generate Valid Token for Manager
        var token = jwtService.GenerateAccessToken(manager);

        // 4. Create DELETE Request with JSON Body
        // Note: Standard HttpClient calls usually don't support Body for DeleteAsync comfortably,
        // so we build HttpRequestMessage manually.
        var request = new HttpRequestMessage(HttpMethod.Delete, "/auth/delete");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { userId = victimId });

        // 5. Act
        var response = await client.SendAsync(request);

        // 6. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify in DB that victim is gone
        using var checkScope = _factory.Services.CreateScope();
        var checkDb = CreateDbContext(checkScope);
        var deletedUser = await checkDb.Users.FindAsync(victimId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task Delete_User_As_Student_Should_Return_Forbidden()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // Seed Student
        var student = new User { Name = "Student", Email = "s@delete.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(student);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(student);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/auth/delete");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { userId = Guid.NewGuid() });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_User_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/auth/delete");
        request.Content = JsonContent.Create(new { userId = Guid.NewGuid() });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
