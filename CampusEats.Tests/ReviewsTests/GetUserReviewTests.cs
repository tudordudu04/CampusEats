using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews;
using CampusEats.Api.Features.Reviews.GetUserReview;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusEats.Tests.ReviewsTests;

public class GetUserReviewTests
{
    [Fact]
    public async Task Handle_Should_Return_Review_When_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetUserReviewHandler(db);

        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed
        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "Alice", Email = "a@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 5m,
            Comment = "Excellent"
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetUserReviewQuery(menuItemId, userId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Excellent", result!.Comment);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Alice", result.UserName);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetUserReviewHandler(db);

        var result = await handler.Handle(new GetUserReviewQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }
}

public class GetUserReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Helper classes for simulating Authentication
    public class TestUserContext
    {
        public Guid UserId { get; set; }
    }

    public class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestUserContext userContext)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userContext.UserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public GetUserReviewEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DB
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetUserReview"));

                // Add Auth simulation
                services.AddScoped(sp => new TestUserContext { UserId = Guid.NewGuid() });
                services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TestUserContext>();
                services.AddScoped(sp => new TestUserContext { UserId = userId });
            });
        });

        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact]
    public async Task Get_My_Review_Should_Return_Ok_When_Exists()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "Me", Email = "me@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = userId, Rating = 4m });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var response = await client.GetAsync($"/api/menu/{menuItemId}/reviews/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(review);
        Assert.Equal(userId, review.UserId);
        Assert.Equal(4m, review.Rating);
    }

    [Fact]
    public async Task Get_My_Review_Should_Return_NotFound_When_Not_Exists()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(Guid.NewGuid()); // Random user
        var response = await client.GetAsync($"/api/menu/{menuItemId}/reviews/mine");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
