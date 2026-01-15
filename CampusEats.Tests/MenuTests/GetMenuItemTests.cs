using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Menu;
using CampusEats.Api.Features.Menu.GetMenuItem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.MenuTests;

public class GetMenuItemTests
{
    [Fact]
    public async Task Handle_Should_Return_Item_When_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Pizza", 20, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new GetMenuItemHandler(db);

        var result = await handler.Handle(new GetMenuItemQuery(id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Pizza", result.Name);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetMenuItemHandler(db);

        var result = await handler.Handle(new GetMenuItemQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }
}

public class GetMenuItemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetMenuItemEndpointTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> SeedMenuItemAsync(string name = "Test Item")
    {
        await using var db = CreateDbContext();
        var entity = new MenuItem(Guid.NewGuid(), name, 10m, "Desc", MenuCategory.PIZZA, null, []);
        db.MenuItems.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    [Fact]
    public async Task GetMenuItem_ExistingId_Should_Return_Ok()
    {
        var id = await SeedMenuItemAsync("SingleItem");
        var response = await _client.GetAsync($"/api/menu/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);
        Assert.Equal("SingleItem", dto.Name);
    }

    [Fact]
    public async Task GetMenuItem_NonExistingId_Should_Return_NotFound()
    {
        var response = await _client.GetAsync($"/api/menu/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}