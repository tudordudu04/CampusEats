using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth;
using CampusEats.Api.Features.Auth.GetAllUsers;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.AuthTests;

public class GetAllUsersTests
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
    public async Task Handle_Should_Return_Ok_With_Users_When_Caller_Is_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal("MANAGER") };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new GetAllUsersHandler(db, http);

        // Seed
        db.Users.AddRange(
            new User { Name = "U1", Email = "e1@t.com", Role = UserRole.STUDENT, PasswordHash = "x" },
            new User { Name = "U2", Email = "e2@t.com", Role = UserRole.WORKER, PasswordHash = "x" }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<UserDto>>>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(2, ok.Value.Count);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        // Unauthenticated principal
        var context = new DefaultHttpContext { User = CreatePrincipal("", isAuthenticated: false) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new GetAllUsersHandler(db, http);

        // Act
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Manager()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = CreatePrincipal("STUDENT") };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new GetAllUsersHandler(db, http);

        // Act
        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>(result);
    }
}

public class GetAllUsersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetAllUsersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetAllUsers"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Get_Users_As_Manager_Should_Return_Ok()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed Manager
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Name = "Manager User",
            Email = "manager@test.com",
            Role = UserRole.MANAGER,
            PasswordHash = "x"
        };
        db.Users.Add(manager);
        
        // 2. Seed other user to fetch
        db.Users.Add(new User { Name = "Student", Email="s@t.com", Role = UserRole.STUDENT, PasswordHash="x" });
        await db.SaveChangesAsync();

        // 3. Generate Valid Token
        var token = jwtService.GenerateAccessToken(manager);
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 4. Act
        var response = await client.SendAsync(request);

        // 5. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
    }

    [Fact]
    public async Task Get_Users_As_Student_Should_Return_Forbidden()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var student = new User { Name = "Std", Email = "std@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(student);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(student);
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Users_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/auth/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
