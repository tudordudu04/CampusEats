using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews;
using CampusEats.Api.Features.Reviews.GetMenuItemReviews;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.ReviewsTests;

public class GetMenuItemReviewsTests
{
    [Fact]
    public async Task Handle_Should_Return_Empty_When_No_Reviews()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemReviewsHandler(db);
        
        var result = await handler.Handle(new GetMenuItemReviewsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_Should_Return_Reviews_Ordered_By_Date_Descending()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemReviewsHandler(db);

        var menuItemId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        // Seed Data
        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.AddRange(
            new User { Id = user1Id, Name = "Alice", Email = "a@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" },
            new User { Id = user2Id, Name = "Bob", Email = "b@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" }
        );

        var oldDate = DateTime.UtcNow.AddDays(-5);
        var newDate = DateTime.UtcNow;

        db.MenuItemReviews.AddRange(
            new MenuItemReview
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItemId,
                UserId = user1Id,
                Rating = 5m,
                Comment = "Old Review",
                CreatedAtUtc = oldDate
            },
            new MenuItemReview
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItemId,
                UserId = user2Id,
                Rating = 3m,
                Comment = "New Review",
                CreatedAtUtc = newDate
            }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetMenuItemReviewsQuery(menuItemId), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        
        // Latest first
        Assert.Equal("New Review", result[0].Comment);
        Assert.Equal("Bob", result[0].UserName);
        Assert.Equal(3m, result[0].Rating);

        // Oldest second
        Assert.Equal("Old Review", result[1].Comment);
        Assert.Equal("Alice", result[1].UserName);
    }
}

public class GetMenuItemReviewsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetMenuItemReviewsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetReviews"));
            });
        });

        _client = _factory.CreateClient();
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task Get_Reviews_Should_Return_Ok_And_List()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "Test User", Email = "t@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 4.5m,
            Comment = "Tasty",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/menu/{menuItemId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();
        
        Assert.NotNull(reviews);
        Assert.Single(reviews);
        Assert.Equal("Test User", reviews[0].UserName);
        Assert.Equal("Tasty", reviews[0].Comment);
    }

    [Fact]
    public async Task Get_Reviews_Should_Return_Empty_List_When_None_Exist()
    {
        var menuItemId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/menu/{menuItemId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();
        
        Assert.NotNull(reviews);
        Assert.Empty(reviews);
    }
}
