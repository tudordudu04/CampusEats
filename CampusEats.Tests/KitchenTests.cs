using CampusEats.Api.Enums;
using CampusEats.Api.Features.Kitchen.UpdateKitchenTask;
using CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;
using CampusEats.Api.Domain;
using CampusEats.Api.Infrastructure.Loyalty;
using NSubstitute;

namespace CampusEats.Tests;

public class KitchenTests
{
    private sealed class FakeLoyaltyService : ILoyaltyService
    {
        public Task AwardPointsForOrder(Guid userId, Guid orderId, decimal total) => Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateKitchenTask_Should_Sync_OrderStatus()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();

        var orderId = new Guid("3f3d9c77-8b8d-4e3e-a5b4-2f4d9e2f4c11");
        var taskId = new Guid("9c1a5fbb-2f3a-4c9a-9c0c-1a2b3c4d5e6f");
        var userId = Guid.NewGuid();

        db.Orders.Add(new Order 
        { 
            Id = orderId, 
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.Preparing,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var loyalty = new FakeLoyaltyService();
        var handler = new UpdateKitchenTaskHandler(db, loyalty);

        var command = new UpdateKitchenTaskCommand(taskId, null, "Preparing", null);
        await handler.Handle(command, CancellationToken.None);

        var order = await db.Orders.FindAsync(orderId);

        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public async Task UpdateKitchenTask_To_Completed_Should_Set_Order_Ready()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Preparing,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        
        var task = new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.Preparing,
            UpdatedAt = DateTime.UtcNow
        };
        db.KitchenTasks.Add(task);
        await db.SaveChangesAsync();
        
        var loyalty = new FakeLoyaltyService();
        var handler = new UpdateKitchenTaskHandler(db, loyalty);
        var command = new UpdateKitchenTaskCommand(taskId, null, "Completed", null);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedOrder = await db.Orders.FindAsync(orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Completed, updatedOrder.Status);
        
        var updatedTask = await db.KitchenTasks.FindAsync(taskId);
        Assert.NotNull(updatedTask);
        Assert.Equal(KitchenTaskStatus.Completed, updatedTask.Status);
    }

    [Fact]
    public async Task UpdateKitchenTask_Should_Update_Notes()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending });
        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.NotStarted,
            Notes = "Original notes",
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        
        var loyalty = new FakeLoyaltyService();
        var handler = new UpdateKitchenTaskHandler(db, loyalty);
        var command = new UpdateKitchenTaskCommand(taskId, null, null, "Updated notes");
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var task = await db.KitchenTasks.FindAsync(taskId);
        Assert.NotNull(task);
        Assert.Equal("Updated notes", task.Notes);
    }

    [Fact]
    public async Task UpdateKitchenTask_Should_Update_Timestamp()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var initialTime = DateTime.UtcNow.AddMinutes(-10);
        
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending });
        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.NotStarted,
            UpdatedAt = initialTime
        });
        await db.SaveChangesAsync();
        
        var loyalty = new FakeLoyaltyService();
        var handler = new UpdateKitchenTaskHandler(db, loyalty);
        var command = new UpdateKitchenTaskCommand(taskId, null, "Preparing", null);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var task = await db.KitchenTasks.FindAsync(taskId);
        Assert.NotNull(task);
        Assert.True(task.UpdatedAt > initialTime);
    }

    [Fact]
    public async Task UpdateKitchenTask_Should_Handle_Status_Transitions()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending });
        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            Status = KitchenTaskStatus.NotStarted,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        
        var loyalty = new FakeLoyaltyService();
        var handler = new UpdateKitchenTaskHandler(db, loyalty);
        
        // Act - NotStarted -> Preparing
        await handler.Handle(new UpdateKitchenTaskCommand(taskId, null, "Preparing", null), CancellationToken.None);
        var task1 = await db.KitchenTasks.FindAsync(taskId);
        Assert.Equal(KitchenTaskStatus.Preparing, task1.Status);
        
        // Act - Preparing -> Completed
        await handler.Handle(new UpdateKitchenTaskCommand(taskId, null, "Completed", null), CancellationToken.None);
        var task2 = await db.KitchenTasks.FindAsync(taskId);
        Assert.Equal(KitchenTaskStatus.Completed, task2.Status);
        
        var finalOrder = await db.Orders.FindAsync(orderId);
        Assert.NotNull(finalOrder);
        Assert.Equal(OrderStatus.Completed, finalOrder.Status);
    }
    
    

    [Fact]
    public async Task UpdateKitchenTask_On_Cancelled_Order_Should_Block_Preparing_Status()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
    
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Cancelled, CreatedAt = DateTime.UtcNow, UserId = Guid.NewGuid() });
        db.KitchenTasks.Add(new KitchenTask { Id = taskId, OrderId = orderId, Status = KitchenTaskStatus.NotStarted });
        await db.SaveChangesAsync();
    
        var handler = new UpdateKitchenTaskHandler(db, Substitute.For<ILoyaltyService>());
    
        var result = await handler.Handle(new UpdateKitchenTaskCommand(taskId, null, "Preparing", null), CancellationToken.None);
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result); // Conform logicii speciale
    }

    [Fact]
    public async Task UpdateKitchenTask_Should_Award_Points_Only_Once()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loyalty = Substitute.For<ILoyaltyService>();
    
        db.Orders.Add(new Order { Id = orderId, UserId = userId, Status = OrderStatus.Pending, LoyaltyPointsAwarded = false, Total = 100m, CreatedAt = DateTime.UtcNow });
        db.KitchenTasks.Add(new KitchenTask { Id = taskId, OrderId = orderId, Status = KitchenTaskStatus.NotStarted });
        await db.SaveChangesAsync();
    
        var handler = new UpdateKitchenTaskHandler(db, loyalty);
    
        await handler.Handle(new UpdateKitchenTaskCommand(taskId, null, "Preparing", null), CancellationToken.None);
        await handler.Handle(new UpdateKitchenTaskCommand(taskId, null, "Preparing", null), CancellationToken.None);
    
        await loyalty.Received(1).AwardPointsForOrder(userId, orderId, 100m); // Verifică flag-ul LoyaltyPointsAwarded
    }
}