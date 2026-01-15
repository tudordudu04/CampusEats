using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.Refresh;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.AuthTests;

public class RefreshTests
{
    class FakeJwtService : IJwtTokenService
    {
        public string GenerateAccessToken(User user) => "new_access_token";
        public (string token, string hash, DateTime expiresAtUtc) GenerateRefreshToken()
            => throw new NotImplementedException(); // Not used in this handler
        public string Hash(string value) => "hashed_" + value;
    }

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
    public async Task Handle_Should_Return_AccessToken_When_Token_Valid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();

        // 1. Setup Context with Cookie
        var rawToken = "valid_token";
        var context = new DefaultHttpContext();
        context.Request.Cookies = new FakeCookieCollection(new() { { "refresh_token", rawToken } });
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new RefreshHandler(db, jwt, http);

        // 2. Seed Data
        var user = new User { Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = jwt.Hash(rawToken),
            RevokedAtUtc = null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        // 3. Act
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        // 4. Assert
        // Fix: Use IsAssignableFrom<IValueHttpResult> to handle anonymous types inside Ok<T>
        var okResult = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IValueHttpResult>(result);

        // Serialize or inspect the value
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("new_access_token", json);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Cookie_Missing()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var handler = new RefreshHandler(db, jwt, http);

        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Token_Revoked()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();

        var rawToken = "revoked_token";
        var context = new DefaultHttpContext();
        context.Request.Cookies = new FakeCookieCollection(new() { { "refresh_token", rawToken } });
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new RefreshHandler(db, jwt, http);

        var user = new User { Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = jwt.Hash(rawToken),
            RevokedAtUtc = DateTime.UtcNow, // Revoked!
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Token_Expired()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = new FakeJwtService();

        var rawToken = "expired_token";
        var context = new DefaultHttpContext();
        context.Request.Cookies = new FakeCookieCollection(new() { { "refresh_token", rawToken } });
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new RefreshHandler(db, jwt, http);

        var user = new User { Name = "Test", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = jwt.Hash(rawToken),
            RevokedAtUtc = null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) // Expired!
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }
}

public class RefreshEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RefreshEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_Refresh"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Refresh_With_Valid_Cookie_Should_Return_New_AccessToken()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed
        var user = new User { Name = "Refresh User", Email = "refresh@test.com", Role = UserRole.STUDENT, PasswordHash = "x" };
        db.Users.Add(user);

        var rawToken = "integration_refresh_token";
        var hash = jwt.Hash(rawToken);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            // Ensure token is valid
            RevokedAtUtc = null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        // 2. Request
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={rawToken}");

        var response = await client.SendAsync(request);

        // 3. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [Fact]
    public async Task Refresh_Without_Cookie_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_With_Invalid_Cookie_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=invalid_or_manipulated_token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
