using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Menu;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.MenuTests;

public class GetAllMenuItemsTests
{
    [Fact]
    public async Task GetAllMenuItems_Should_Return_All_Items()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        db.MenuItems.AddRange(
            new MenuItem(id1, "Pizza", 20, "Desc", MenuCategory.PIZZA, null, []),
            new MenuItem(id2, "Burger", 15, "Desc", MenuCategory.BURGER, null, [])
        );


        await db.SaveChangesAsync();

        var handler = new GetAllMenuItemsHandler(db);
        var result = await handler.Handle(new GetAllMenuItemsQuery(), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);

        var pizza = result.First(x => x.Id == id1);
        Assert.Equal("Pizza", pizza.Name);
    }
}

public class GetAllMenuItemsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetAllMenuItemsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private async Task SeedMenuItemsAsync()
    {
        await using var db = CreateDbContext();
        db.MenuItems.AddRange(
            new MenuItem(Guid.NewGuid(), "Pizza", 20m, "Desc", MenuCategory.PIZZA, null, []),
            new MenuItem(Guid.NewGuid(), "Burger", 15m, "Desc", MenuCategory.BURGER, null, [])
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllMenuItems_Should_Return_Ok_And_List()
    {
        await SeedMenuItemsAsync();

        var response = await _client.GetAsync("/api/menu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>();
        Assert.NotNull(items);
        Assert.True(items!.Count >= 2);
        Assert.Equal(MenuCategory.PIZZA, items.First(x => x.Name == "Pizza").Category);
    }
}
