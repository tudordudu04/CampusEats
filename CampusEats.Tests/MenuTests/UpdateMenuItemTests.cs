using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.MenuTests;

public class UpdateMenuItemTests
{
    [Fact]
    public async Task Handle_Should_Update_Existing_Item_And_Return_True()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Old", 10, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(id, "New", 15, "NewDesc", MenuCategory.BURGER, null, []);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result);
        var entity = await db.MenuItems.FirstAsync(m => m.Id == id);
        Assert.Equal("New", entity.Name);
        Assert.Equal(15, entity.Price);
        Assert.Equal("NewDesc", entity.Description);
        Assert.Equal(MenuCategory.BURGER, entity.Category);
    }

    [Fact]
    public async Task Handle_Should_Trim_Name_And_Description()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Original", 10, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(id, "  Trimmed Name  ", 20, "  Trimmed Desc  ", MenuCategory.BURGER, "  image.jpg  ", ["peanuts"]);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result);
        var entity = await db.MenuItems.FirstAsync(m => m.Id == id);
        Assert.Equal("Trimmed Name", entity.Name);
        Assert.Equal("Trimmed Desc", entity.Description);
        Assert.Equal("image.jpg", entity.ImageUrl);
        Assert.Single(entity.Allergens);
    }

    [Fact]
    public async Task Handle_Should_Handle_Null_Optional_Fields()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Original", 10, "Desc", MenuCategory.PIZZA, "old.jpg", ["old"]));
        await db.SaveChangesAsync();

        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(id, "Updated", 15, null, MenuCategory.DRINK, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result);
        var entity = await db.MenuItems.FirstAsync(m => m.Id == id);
        Assert.Equal("Updated", entity.Name);
        Assert.Null(entity.Description);
        Assert.Null(entity.ImageUrl);
        Assert.Empty(entity.Allergens);
    }

    [Fact]
    public async Task Handle_Should_Update_All_Properties_Including_ImageUrl()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Old", 10, "OldDesc", MenuCategory.PIZZA, "old.jpg", ["old"]));
        await db.SaveChangesAsync();

        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(
            id, 
            "NewName", 
            25.99m, 
            "NewDescription", 
            MenuCategory.BURGER, 
            "https://example.com/new.jpg", 
            new[] { "gluten", "dairy" }
        );

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result);
        var entity = await db.MenuItems.FirstAsync(m => m.Id == id);
        Assert.Equal("NewName", entity.Name);
        Assert.Equal(25.99m, entity.Price);
        Assert.Equal("NewDescription", entity.Description);
        Assert.Equal(MenuCategory.BURGER, entity.Category);
        Assert.Equal("https://example.com/new.jpg", entity.ImageUrl);
        Assert.Equal(2, entity.Allergens.Length);
        Assert.Contains("gluten", entity.Allergens);
        Assert.Contains("dairy", entity.Allergens);
    }

    [Fact]
    public async Task Handle_Should_SaveChanges_And_Persist_Updates()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var id = Guid.NewGuid();
        db.MenuItems.Add(new MenuItem(id, "Original", 10, null, MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();

        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(id, "Updated", 20, "New Description", MenuCategory.SALAD, null, []);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result);
        
        // Verify changes are persisted by detaching and re-querying
        db.Entry(await db.MenuItems.FindAsync(id)).State = EntityState.Detached;
        var requeried = await db.MenuItems.AsNoTracking().FirstAsync(m => m.Id == id);
        Assert.Equal("Updated", requeried.Name);
        Assert.Equal(20, requeried.Price);
        Assert.Equal(MenuCategory.SALAD, requeried.Category);
    }

    [Fact]
    public async Task Handle_Should_Return_False_When_Item_Not_Found()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new UpdateMenuItemHandler(db);
        var cmd = new UpdateMenuItemCommand(Guid.NewGuid(), "X", 1, null, MenuCategory.PIZZA, null, []);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result);
    }
}

public class UpdateMenuItemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UpdateMenuItemEndpointTests(WebApplicationFactory<Program> factory)
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
        db.MenuItems.Add(new MenuItem(id, "Original", 10m, "Desc", MenuCategory.PIZZA, null, []));
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class UpdateMenuItemRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Updated Name";
        public decimal Price { get; set; } = 15m;
        public string? Description { get; set; } = "Updated Desc";
        public MenuCategory Category { get; set; } = MenuCategory.PIZZA;
        public string? ImageUrl { get; set; }
        public string[] Allergens { get; set; } = [];
    }

    [Fact]
    public async Task UpdateMenuItem_IdMismatch_Should_Return_BadRequest()
    {
        var existingId = await SeedMenuItemAsync();
        var body = new UpdateMenuItemRequest { Id = existingId };
        var routeId = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/menu/{routeId}", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMenuItem_ExistingId_Should_Return_Ok()
    {
        var id = await SeedMenuItemAsync();
        var body = new UpdateMenuItemRequest { Id = id, Name = "NewName" };

        var response = await _client.PutAsJsonAsync($"/api/menu/{id}", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMenuItem_NonExistingId_Should_Return_NotFound()
    {
        var id = Guid.NewGuid();
        var body = new UpdateMenuItemRequest { Id = id, Name = "DoesNotExist" };

        var response = await _client.PutAsJsonAsync($"/api/menu/{id}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}