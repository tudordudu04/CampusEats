using CampusEats.Api.Enums;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using CampusEats.Api.Domain;
using Microsoft.AspNetCore.Http;
using Xunit;


namespace CampusEats.Tests;

public class MenuTests
{
    [Fact]
    public async Task CreateMenuItem_Should_Add_Item_To_Database()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new CreateMenuItemHandler(db, new HttpContextAccessor());
        
        var command = new CreateMenuItemCommand(
            Name: "Test Item",
            Description: "A delicious test item",
            Price: 9.99m,
            Category: MenuCategory.PIZZA,
            ImageUrl:null,
            Allergens: new[] {"Gluten"}
        );
        
        var resultId = await handler.Handle(command, CancellationToken.None);
        
        var itemInDb = await db.MenuItems.FindAsync(resultId);
        Assert.NotNull(itemInDb);
        Assert.Equal("Test Item", itemInDb.Name);
    }

    [Fact]
    public async Task DeleteMenuItem_Should_Return_True_When_Exists()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        //Aici trebuie cu un Guid deja generat
        var item = new MenuItem(Guid.NewGuid(), "Burger", 20, null, MenuCategory.BURGER, null, []);
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        
        var handler = new DeleteMenuItemHandler(db);

        var result = await handler.Handle(new DeleteMenuItemCommand(item.Id), CancellationToken.None);
        
        Assert.True(result);
        Assert.Null(await db.MenuItems.FindAsync(item.Id));
    }
    
    
}