using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace CampusEats.Tests.AuthTests;

public class LoginUserTests
{
    // Stubs for unit testing logic in isolation
    class FakePasswordService(bool result) : IPasswordService
    {
        public string Hash(User user, string password) => "hashed_" + password;
        public bool Verify(User user, string hashed, string password) => result;
    }

    class FakeJwtService : IJwtTokenService
    {
        public string GenerateAccessToken(User user) => "fake_access_token";
        public (string token, string hash, DateTime expiresAtUtc) GenerateRefreshToken()
            => ("fresh_rt", "hash", DateTime.UtcNow.AddDays(7));
        public string Hash(string value) => "hashed_" + value;
    }

    [Fact]
    public async Task Handle_Should_Return_Ok_And_Token_When_Credentials_Valid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var passwords = new FakePasswordService(true);
        var jwt = new FakeJwtService();
        var context = new DefaultHttpContext();
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new LoginUserHandler(db, passwords, jwt, http);

        var user = new User { Email = "test@t.com", Name = "Test", PasswordHash = "hash", Role = UserRole.STUDENT };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var cmd = new LoginUserCommand("test@t.com", "any_pass");
        var result = await handler.Handle(cmd, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<AuthResultDto>>(result);
        Assert.Equal("fake_access_token", okResult.Value!.AccessToken);

        var cookie = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("refresh_token=fresh_rt", cookie);
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_User_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var passwords = new FakePasswordService(true);
        var jwt = new FakeJwtService();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var handler = new LoginUserHandler(db, passwords, jwt, http);

        var cmd = new LoginUserCommand("unknown@t.com", "pass");
        var result = await handler.Handle(cmd, CancellationToken.None);

        var badRequest = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result);
        Assert.Equal("Invalid email or password.", badRequest.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Password_Invalid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var passwords = new FakePasswordService(false);
        var jwt = new FakeJwtService();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var handler = new LoginUserHandler(db, passwords, jwt, http);

        db.Users.Add(new User { Email = "test@t.com", Name = "Test", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var cmd = new LoginUserCommand("test@t.com", "wrong_pass");
        var result = await handler.Handle(cmd, CancellationToken.None);

        var badRequest = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result);
        Assert.Equal("Invalid email or password.", badRequest.Value);
    }

    [Fact]
    public async Task Handle_Should_Revoke_Existing_RefreshTokens()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var passwords = new FakePasswordService(true);
        var jwt = new FakeJwtService();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var handler = new LoginUserHandler(db, passwords, jwt, http);

        var userId = Guid.NewGuid();
        // Fix: Added 'Name' property which is required by EF Core model
        db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@t.com", PasswordHash = "hash" });

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RevokedAtUtc = null,
            TokenHash = "old_hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var cmd = new LoginUserCommand("test@t.com", "pass");
        await handler.Handle(cmd, CancellationToken.None);

        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        Assert.Equal(2, tokens.Count);

        var oldToken = tokens.First(t => t.TokenHash == "old_hash");
        Assert.NotNull(oldToken.RevokedAtUtc);

        var newToken = tokens.First(t => t.TokenHash != "old_hash");
        Assert.Null(newToken.RevokedAtUtc);
    }
}

public class LoginUserEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoginUserEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_Login"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task Login_Valid_User_Should_Return_Ok_And_Set_Cookie()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        
        // Fix: Instantiate PasswordHasher directly instead of resolving from DI
        // This avoids the 'No service for type' error if it's not registered as a standalone service in Program.cs
        var passwordHasher = new PasswordHasher<User>();

        var email = "login_success@test.com";
        var password = "SecurePassword1!";

        var user = new User { Name = "Test", Email = email, Role = UserRole.STUDENT };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var command = new LoginUserCommand(email, password);
        var response = await client.PostAsJsonAsync("/auth/login", command);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));

        var setCookieHeader = response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie");
        Assert.NotNull(setCookieHeader.Value);
        Assert.Contains(setCookieHeader.Value, c => c.StartsWith("refresh_token="));
    }

    [Fact]
    public async Task Login_Invalid_Password_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        
        // Fix: Instantiate PasswordHasher directly
        var passwordHasher = new PasswordHasher<User>();

        var email = "login_fail@test.com";

        var user = new User { Name = "Test", Email = email, Role = UserRole.STUDENT };
        user.PasswordHash = passwordHasher.HashPassword(user, "CorrectPassword");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var command = new LoginUserCommand(email, "WrongPassword");
        var response = await client.PostAsJsonAsync("/auth/login", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistent_User_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        var command = new LoginUserCommand("ghost@test.com", "pass");

        var response = await client.PostAsJsonAsync("/auth/login", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Invalid_Data_Should_Return_BadRequest_Validation()
    {
        var client = _factory.CreateClient();

        var command = new LoginUserCommand("email@test.com", "");
        var response = await client.PostAsJsonAsync("/auth/login", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var command2 = new LoginUserCommand("not-an-email", "pass");
        var response2 = await client.PostAsJsonAsync("/auth/login", command2);

        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }
}
