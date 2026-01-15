using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews.DeleteReview;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusEats.Tests.ReviewsTests;

public class DeleteReviewTests
{
    [Fact]
    public async Task Handle_Should_Delete_Review_When_User_Owns_It()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteReviewHandler(db);

        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "User", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = reviewId,
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 5m,
            Comment = "Good"
        });
        await db.SaveChangesAsync();

        await handler.Handle(new DeleteReviewCommand(reviewId, userId), CancellationToken.None);

        var exists = await db.MenuItemReviews.AnyAsync(r => r.Id == reviewId);
        Assert.False(exists);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Review_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteReviewHandler(db);

        var cmd = new DeleteReviewCommand(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_User_Does_Not_Own_Review()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteReviewHandler(db);

        var reviewId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = ownerId, Name = "Owner", Email = "o@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = reviewId,
            MenuItemId = menuItemId,
            UserId = ownerId,
            Rating = 5m
        });
        await db.SaveChangesAsync();

        var cmd = new DeleteReviewCommand(reviewId, otherUserId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }
}

public class DeleteReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

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

    public DeleteReviewEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_DeleteReviews"));

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
    public async Task Delete_Review_Should_Return_NoContent()
    {
        await using var db = CreateDbContext();
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "User", Email = "u@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = reviewId,
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 5m
        });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var response = await client.DeleteAsync($"/api/reviews/{reviewId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verifyDb = CreateDbContext();
        Assert.False(await verifyDb.MenuItemReviews.AnyAsync(r => r.Id == reviewId));
    }

    [Fact]
    public async Task Delete_Review_Of_Another_User_Should_Return_Forbid()
    {
        await using var db = CreateDbContext();
        var reviewId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var hackerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = ownerId, Name = "Owner", Email = "o@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = reviewId,
            MenuItemId = menuItemId,
            UserId = ownerId,
            Rating = 5m
        });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(hackerId);
        var response = await client.DeleteAsync($"/api/reviews/{reviewId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExisting_Review_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid());
        var response = await client.DeleteAsync($"/api/reviews/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
