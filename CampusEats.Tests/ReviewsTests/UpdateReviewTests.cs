using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews;
using CampusEats.Api.Features.Reviews.UpdateReview;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusEats.Tests.ReviewsTests;

public class UpdateReviewTests
{
    [Fact]
    public async Task Handle_Should_Update_Review_When_User_Owns_It()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new UpdateReviewHandler(db);

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
            Comment = "Old Comment",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var cmd = new UpdateReviewCommand(reviewId, userId, 3.5m, "New Comment");
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3.5m, result.Rating);
        Assert.Equal("New Comment", result.Comment);
        Assert.True(result.UpdatedAtUtc > result.CreatedAtUtc);

        var dbReview = await db.MenuItemReviews.FindAsync(reviewId);
        Assert.Equal(3.5m, dbReview!.Rating);
        Assert.Equal("New Comment", dbReview.Comment);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Review_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new UpdateReviewHandler(db);

        var cmd = new UpdateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), 5m, "Comment");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_User_Does_Not_Own_Review()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new UpdateReviewHandler(db);

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

        var cmd = new UpdateReviewCommand(reviewId, hackerId, 1m, "Hacked");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Rating_Is_Invalid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new UpdateReviewHandler(db);

        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        
        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "User", Email = "u@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview { Id = reviewId, MenuItemId = menuItemId, UserId = userId, Rating = 5m });
        await db.SaveChangesAsync();

        var cmd = new UpdateReviewCommand(reviewId, userId, 6.0m, "Invalid Rating");

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(cmd, CancellationToken.None));
    }
}

public class UpdateReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
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

    public UpdateReviewEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_UpdateReviews"));

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
    public async Task Update_Review_Should_Return_Ok_And_Updated_Dto()
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
            Rating = 5m,
            Comment = "Old"
        });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var request = new UpdateReviewRequest(4.0m, "Updated");
        var response = await client.PutAsJsonAsync($"/api/reviews/{reviewId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(dto);
        Assert.Equal(4.0m, dto.Rating);
        Assert.Equal("Updated", dto.Comment);
    }

    [Fact]
    public async Task Update_Review_Of_Another_User_Should_Return_Forbid()
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
        var request = new UpdateReviewRequest(1.0m, "Hacked");
        var response = await client.PutAsJsonAsync($"/api/reviews/{reviewId}", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_Review_With_Invalid_Rating_Should_Return_BadRequest()
    {
        await using var db = CreateDbContext();
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "User", Email = "u@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview { Id = reviewId, MenuItemId = menuItemId, UserId = userId, Rating = 5m });
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userId);
        var request = new UpdateReviewRequest(6.0m, "Invalid");
        var response = await client.PutAsJsonAsync($"/api/reviews/{reviewId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExisting_Review_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid());
        var request = new UpdateReviewRequest(4.0m, "Updated");
        var response = await client.PutAsJsonAsync($"/api/reviews/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
