using Xunit;
using CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class DeleteKitchenTaskHandlerTests
{
    [Fact]
    public async Task DeleteKitchenTask_Should_Return_NotFound_When_Task_Does_Not_Exist()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new DeleteKitchenTaskHandler(db);
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteKitchenTaskCommand(nonExistentId);

        var result = await handler.Handle(command, CancellationToken.None);

        var notFoundResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>(result);
        Assert.Contains(nonExistentId.ToString(), notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteKitchenTask_Should_Delete_Task_Successfully()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });

        var task = new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.Preparing,
            Notes = "Test task",
            UpdatedAt = DateTime.UtcNow
        };
        db.KitchenTasks.Add(task);
        await db.SaveChangesAsync();

        var handler = new DeleteKitchenTaskHandler(db);
        var command = new DeleteKitchenTaskCommand(taskId);

        var result = await handler.Handle(command, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>(result);
        Assert.Contains(taskId.ToString(), okResult.Value);
        Assert.Contains("deleted", okResult.Value);

        var deletedTask = await db.KitchenTasks.FindAsync(taskId);
        Assert.Null(deletedTask);
    }

    [Fact]
    public async Task DeleteKitchenTask_Should_Remove_Task_From_Database()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();

        var orderId = Guid.NewGuid();
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Preparing,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.AddRange(
            new KitchenTask
            {
                Id = task1Id,
                OrderId = orderId,
                AssignedTo = Guid.NewGuid(),
                Status = KitchenTaskStatus.Preparing,
                UpdatedAt = DateTime.UtcNow
            },
            new KitchenTask
            {
                Id = task2Id,
                OrderId = orderId,
                AssignedTo = Guid.NewGuid(),
                Status = KitchenTaskStatus.NotStarted,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var initialCount = await db.KitchenTasks.CountAsync();
        Assert.Equal(2, initialCount);

        var handler = new DeleteKitchenTaskHandler(db);
        var command = new DeleteKitchenTaskCommand(task1Id);

        await handler.Handle(command, CancellationToken.None);

        var finalCount = await db.KitchenTasks.CountAsync();
        Assert.Equal(1, finalCount);

        var remainingTask = await db.KitchenTasks.FindAsync(task2Id);
        Assert.NotNull(remainingTask);
    }
}
