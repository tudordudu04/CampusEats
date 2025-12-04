using System.Security.Claims;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Features.Orders.PlaceOrder;
using CampusEats.Api.Domain;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CampusEats.Api.Enums;
using Microsoft.AspNetCore.Http;
using NSubstitute;


namespace CampusEats.Tests;

public class OrderTests
{
    [Fact]
    public async Task PlaceOrder_Should_Calculate_Total_And_Create_KitchenTask()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();

        var pizza = new MenuItem(Guid.NewGuid(), "Pizza", 20m, null, MenuCategory.PIZZA, null, []);
        var cola = new MenuItem(Guid.NewGuid(), "Cola", 5m, null, MenuCategory.DRINK, null, []);
        db.MenuItems.AddRange(pizza, cola);
        await db.SaveChangesAsync();
        
        var userId = Guid.NewGuid();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        }));
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = userClaims });
        var handler = new PlaceOrderHandler(db, httpContextAccessor);

        var command = new PlaceOrderCommand(new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto>
            {
                new() { MenuItemId = pizza.Id, Quantity = 2 },
                new() { MenuItemId = cola.Id, Quantity = 1 }
            }
        });
        
        
        var orderId = await handler.Handle(command, CancellationToken.None);
        
        var order = await db.Orders.FindAsync(orderId);
        Assert.Equal(45m, order.Total);
        Assert.Equal(userId, order.UserId);
        
        var kitchenTask = await db.KitchenTasks.FirstOrDefaultAsync(kt => kt.OrderId == orderId);
        Assert.NotNull(kitchenTask);
        Assert.Equal(KitchenTaskStatus.NotStarted, kitchenTask.Status);
    }
    
}