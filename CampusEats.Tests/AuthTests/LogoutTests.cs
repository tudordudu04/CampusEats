using System.Net;
using System.Net.Http.Headers;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.Logout;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace CampusEats.Tests.AuthTests;

public class LogoutTests
{
    // Fake JWT Service matching the logic used in previous tests
    class FakeJwtService : IJwtTokenService
    {
        public string GenerateAccessToken(User user) => "fake_access_token";
        public (string token, string hash, DateTime expiresAtUtc) GenerateRefreshToken()
            => ("fresh_rt", "hashed_fresh_rt", DateTime.UtcNow.AddDays(7));

        public string Hash(string value) => "hashed_" + value;
    }

    // specific implementation to mock cookies in DefaultHttpContext
    class FakeCookieCollection(Dictionary<string, string> cookies) : IRequestCookieCollection
    {
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => cookies.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public ICollection<string> Keys => cookies.Keys;

        public bool ContainsKey(string key) => cookies.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => cookies.TryGetValue(key, out value!);

        public int Count => cookies.Count;
        public string this[string key] => cookies[key];
    }

    [Fact]
    public async Task Handle_Should_Revoke_Token_When_Cookie_Exists()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();

        // Setup HttpContext with Cookie
        var context = new DefaultHttpContext();
        var rawToken = "my_refresh_token";
        context.Request.Cookies = new FakeCookieCollection(new Dictionary<string, string>
        {
            { "refresh_token", rawToken }
        });

        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new LogoutHandler(db, jwt, http);

        // Seed Data
        var user = new User { Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(user);

        // Token matches the hash of "my_refresh_token"
        var tokenHash = jwt.Hash(rawToken);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = null
        });
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        // Assert Result
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NoContent>(result);

        // Assert DB side effect (Token Revoked)
        var tokenInDb = await db.RefreshTokens.FirstAsync();
        Assert.NotNull(tokenInDb.RevokedAtUtc);

        // Assert Cookie cleared
        var cookieHeader = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("refresh_token=;", cookieHeader);
        
        // Fix: Use StringComparison.OrdinalIgnoreCase because DefaultHttpContext outputs lowercase "expires"
        Assert.Contains("Expires=Thu, 01 Jan 1970", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_Should_Return_NoContent_When_No_Cookie_Present()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();

        // Empty context
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var handler = new LogoutHandler(db, jwt, http);

        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NoContent>(result);
    }
}

public class LogoutEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LogoutEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_Logout"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Logout_Should_Revoke_Token_And_Clear_Cookie()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed User and Token
        var user = new User { Name = "Logout User", Email = "logout@test.com", Role = UserRole.STUDENT, PasswordHash = "hash" };
        db.Users.Add(user);

        var rawToken = "integration_test_token";
        var hash = jwtService.Hash(rawToken);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = null
        });
        await db.SaveChangesAsync();

        // 2. Prepare Request with Cookie
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("Cookie", $"refresh_token={rawToken}");

        // 3. Act
        var response = await client.SendAsync(request);

        // 4. Assert Response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert Set-Cookie (Deletion)
        var setCookie = response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie");
        Assert.NotNull(setCookie.Value);
        Assert.Contains(setCookie.Value, c => 
            c.StartsWith("refresh_token=;") && 
            c.Contains("expires=", StringComparison.OrdinalIgnoreCase)
        );

        // 5. Assert DB
        // Use a new context instance to ensure we fetch fresh data
        using var scope2 = _factory.Services.CreateScope();
        var db2 = CreateDbContext(scope2);
        var tokenInDb = await db2.RefreshTokens.FirstAsync(t => t.UserId == user.Id);

        Assert.NotNull(tokenInDb.RevokedAtUtc);
    }

    [Fact]
    public async Task Logout_Without_Cookie_Should_Return_NoContent()
    {
        var client = _factory.CreateClient();

        // No Cookie Accessor
        var response = await client.PostAsync("/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
