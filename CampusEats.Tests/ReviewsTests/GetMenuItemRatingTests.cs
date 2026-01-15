using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Reviews;
using CampusEats.Api.Features.Reviews.GetMenuItemRating;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.ReviewsTests;

public class GetMenuItemRatingTests
{
    [Fact]
    public async Task Handle_Should_Return_Zero_When_No_Reviews()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemRatingHandler(db);
        var menuItemId = Guid.NewGuid();

        var result = await handler.Handle(new GetMenuItemRatingQuery(menuItemId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(menuItemId, result.MenuItemId);
        Assert.Equal(0, result.AverageRating);
        Assert.Equal(0, result.TotalReviews);
    }

    [Fact]
    public async Task Handle_Should_Return_Correct_Average_And_Count()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemRatingHandler(db);

        var menuItemId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.AddRange(
            new User { Id = user1Id, Name = "U1", Email = "u1@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" },
            new User { Id = user2Id, Name = "U2", Email = "u2@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" }
        );

        db.MenuItemReviews.AddRange(
            new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = user1Id, Rating = 5m },
            new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = user2Id, Rating = 4m }
        );
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetMenuItemRatingQuery(menuItemId), CancellationToken.None);

        Assert.Equal(4.5m, result.AverageRating);
        Assert.Equal(2, result.TotalReviews);
    }

    [Fact]
    public async Task Handle_Should_Round_Average_Correctly()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemRatingHandler(db);

        var menuItemId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Burger", 10m, "Desc", MenuCategory.BURGER, null, []));
        db.Users.AddRange(
            new User { Id = user1, Name = "1", Email = "1@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" },
            new User { Id = user2, Name = "2", Email = "2@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" },
            new User { Id = user3, Name = "3", Email = "3@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" }
        );

        db.MenuItemReviews.AddRange(
            new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = user1, Rating = 5m }, // 5
            new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = user2, Rating = 3m }, // 3
            new MenuItemReview { Id = Guid.NewGuid(), MenuItemId = menuItemId, UserId = user3, Rating = 3m }  // 3 -> Sum 11, Avg 3.66...
        );
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetMenuItemRatingQuery(menuItemId), CancellationToken.None);

        Assert.Equal(3.7m, result.AverageRating);
        Assert.Equal(3, result.TotalReviews);
    }
}

public class GetMenuItemRatingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetMenuItemRatingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_GetRating"));
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
    public async Task Get_Rating_Should_Return_Ok_And_Dto()
    {
        await using var db = CreateDbContext();
        var menuItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.MenuItems.Add(new MenuItem(menuItemId, "Pizza", 10m, "Desc", MenuCategory.PIZZA, null, []));
        db.Users.Add(new User { Id = userId, Name = "User", Email = "u@t.com", Role = UserRole.STUDENT, PasswordHash = "hash" });
        db.MenuItemReviews.Add(new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = 4m
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/menu/{menuItemId}/rating");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<MenuItemRatingDto>();
        Assert.NotNull(dto);
        Assert.Equal(menuItemId, dto.MenuItemId);
        Assert.Equal(4m, dto.AverageRating);
        Assert.Equal(1, dto.TotalReviews);
    }

    [Fact]
    public async Task Get_Rating_For_NonExisting_Item_Should_Return_Zero_Values()
    {
        var menuItemId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/menu/{menuItemId}/rating");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<MenuItemRatingDto>();
        Assert.NotNull(dto);
        Assert.Equal(menuItemId, dto.MenuItemId);
        Assert.Equal(0, dto.AverageRating);
        Assert.Equal(0, dto.TotalReviews);
    }
}
