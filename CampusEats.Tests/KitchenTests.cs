using CampusEats.Api.Enums;
using CampusEats.Api.Features.Kitchen.UpdateKitchenTask;
using CampusEats.Api.Domain;
using Xunit;

namespace CampusEats.Tests;

public class KitchenTests
{
    [Fact]
    public async Task UpdateKitchenTask_Should_Sync_OrderStatus()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        
        db.Orders.Add(new Order{Id = orderId, Status = OrderStatus.Pending });
        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.Preparing,
            UpdatedAt = DateTime.UtcNow
            
        });
        await db.SaveChangesAsync();
        var handler = new UpdateKitchenTaskHandler(db);
        
        var command = new UpdateKitchenTaskCommand(taskId, null, "Preparing",null);
        await handler.Handle(command, CancellationToken.None);
        
        var order = await db.Orders.FindAsync(orderId);
        
        
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }
}