using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews;
using CampusEats.Api.Features.Reviews.AddReview;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusEats.Tests.ReviewsTests;

public class AddReviewTests
{
    [Fact]
    public async Task Handle_Should_Create_Review_In_Db()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new AddReviewHandler(db);

        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        // Fix: PasswordHash is required
        db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@test.com", Role = UserRole.STUDENT, PasswordHash = "dummyHash" });
        await db.SaveChangesAsync();

        var cmd = new AddReviewCommand(menuItemId, userId, 4.5m, "Good pizza");

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4.5m, result.Rating);
        Assert.Equal("Good pizza", result.Comment);

        var entity = await db.MenuItemReviews.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(entity);
        Assert.Equal(menuItemId, entity.MenuItemId);
        Assert.Equal(userId, entity.UserId);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Review_Already_Exists()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new AddReviewHandler(db);

        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 5m
        });
        await db.SaveChangesAsync();

        var cmd = new AddReviewCommand(menuItemId, userId, 3m, "New comment");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Rating_Invalid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new AddReviewHandler(db);

        var cmd = new AddReviewCommand(Guid.NewGuid(), Guid.NewGuid(), 6m, "Invalid");

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_MenuItem_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new AddReviewHandler(db);

        var cmd = new AddReviewCommand(Guid.NewGuid(), Guid.NewGuid(), 4m, "Comment");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
    }
}

public class AddReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    // Helper class to inject dynamic UserIds without reregistering auth schemes
    public class TestUserContext
    {
        public Guid UserId { get; set; }
    }

    // Auth Handler that reads from the Scoped TestUserContext
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

    public AddReviewEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_Reviews"));

                // Register the Context service
                services.AddScoped(sp => new TestUserContext { UserId = Guid.NewGuid() });

                // Register Auth Scheme ONCE
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
                // Override ONLY the user context for this specific client
                services.RemoveAll<TestUserContext>();
                services.AddScoped(sp => new TestUserContext { UserId = userId });
            });
        });

        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact]
    public async Task Post_Review_Should_Return_Created_And_Dto()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        // Fix: PasswordHash added
        db.Users.Add(new User { Id = userId, Name = "User", Email = "u@test.com", Role = UserRole.STUDENT, PasswordHash = "dummyHash" });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var request = new AddReviewRequest(menuItemId, 5m, "Great");

        var response = await client.PostAsJsonAsync("/api/reviews", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(dto);
        Assert.Equal(menuItemId, dto.MenuItemId);
        Assert.Equal(5m, dto.Rating);
    }

    [Fact]
    public async Task Post_Review_When_Duplicate_Should_Return_BadRequest()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 5m
        });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var request = new AddReviewRequest(menuItemId, 4m, "Update");

        var response = await client.PostAsJsonAsync("/api/reviews", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
