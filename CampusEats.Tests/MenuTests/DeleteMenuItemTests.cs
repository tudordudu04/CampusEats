using System.Net;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.MenuTests;

public class DeleteMenuItemTests
{
    [Fact]
    public async Task Handle_Should_Delete_Existing_Item_And_Return_True()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "ToDelete", 10, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new DeleteMenuItemHandler(db);

        var result = await handler.Handle(new DeleteMenuItemCommand(id), CancellationToken.None);

        Assert.True(result);
        Assert.False(db.MenuItems.Any(m => m.Id == id));
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_Item_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteMenuItemHandler(db);

        var result = await handler.Handle(new DeleteMenuItemCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }
}

public class DeleteMenuItemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeleteMenuItemEndpointTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> SeedMenuItemAsync()
    {
        await using var db = CreateDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "ToDelete", 10m, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task DeleteMenuItem_ExistingId_Should_Return_NoContent()
    {
        var id = await SeedMenuItemAsync();
        var response = await _client.DeleteAsync($"/api/menu/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMenuItem_NonExistingId_Should_Return_NotFound()
    {
        var response = await _client.DeleteAsync($"/api/menu/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}