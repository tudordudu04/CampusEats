using Xunit;
using CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;
using CampusEats.Api.Features.Kitchen;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class GetAllKitchenTasksHandlerTests
{
    [Fact]
    public async Task GetAllKitchenTasks_Should_Return_Empty_List_When_No_Tasks_Exist()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetAllKitchenTasksHandler(db);
        var query = new GetAllKitchenTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Empty(okResult.Value);
    }

    [Fact]
    public async Task GetAllKitchenTasks_Should_Return_All_Tasks_Regardless_Of_Status()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var orderId3 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.AddRange(
            new Order { Id = orderId1, UserId = userId, Status = OrderStatus.Pending, Total = 50m, CreatedAt = DateTime.UtcNow },
            new Order { Id = orderId2, UserId = userId, Status = OrderStatus.Preparing, Total = 75m, CreatedAt = DateTime.UtcNow },
            new Order { Id = orderId3, UserId = userId, Status = OrderStatus.Completed, Total = 60m, CreatedAt = DateTime.UtcNow }
        );

        var task1 = new KitchenTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId1,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.NotStarted,
            Notes = "Task 1",
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new KitchenTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId2,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.Preparing,
            Notes = "Task 2",
            UpdatedAt = DateTime.UtcNow
        };

        var task3 = new KitchenTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId3,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.Completed,
            Notes = "Task 3",
            UpdatedAt = DateTime.UtcNow
        };

        db.KitchenTasks.AddRange(task1, task2, task3);
        await db.SaveChangesAsync();

        var handler = new GetAllKitchenTasksHandler(db);
        var query = new GetAllKitchenTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(3, okResult.Value.Count);
        
        // Verify all three tasks with different statuses are returned
        Assert.Contains(okResult.Value, dto => dto.Id == task1.Id && dto.Status == "NotStarted");
        Assert.Contains(okResult.Value, dto => dto.Id == task2.Id && dto.Status == "Preparing");
        Assert.Contains(okResult.Value, dto => dto.Id == task3.Id && dto.Status == "Completed");
    }

    [Fact]
    public async Task GetAllKitchenTasks_Should_Include_OrderStatus_From_Join()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.AddRange(
            new Order { Id = orderId1, UserId = userId, Status = OrderStatus.Pending, Total = 50m, CreatedAt = DateTime.UtcNow },
            new Order { Id = orderId2, UserId = userId, Status = OrderStatus.Confirmed, Total = 75m, CreatedAt = DateTime.UtcNow }
        );

        db.KitchenTasks.AddRange(
            new KitchenTask
            {
                Id = Guid.NewGuid(),
                OrderId = orderId1,
                AssignedTo = Guid.NewGuid(),
                Status = KitchenTaskStatus.NotStarted,
                UpdatedAt = DateTime.UtcNow
            },
            new KitchenTask
            {
                Id = Guid.NewGuid(),
                OrderId = orderId2,
                AssignedTo = Guid.NewGuid(),
                Status = KitchenTaskStatus.Completed,
                UpdatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var handler = new GetAllKitchenTasksHandler(db);
        var query = new GetAllKitchenTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.Equal(2, okResult.Value!.Count);
        
        // Verify OrderStatus is correctly populated from the join
        Assert.Contains(okResult.Value, dto => dto.OrderId == orderId1 && dto.OrderStatus == "Pending");
        Assert.Contains(okResult.Value, dto => dto.OrderId == orderId2 && dto.OrderStatus == "Confirmed");
    }

    [Fact]
    public async Task GetAllKitchenTasks_Should_Map_All_DTO_Properties_Correctly()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 1, 11, 14, 45, 0);

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Preparing,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            AssignedTo = assignedToId,
            Status = KitchenTaskStatus.Preparing,
            Notes = "Special preparation notes",
            UpdatedAt = updatedAt
        });

        await db.SaveChangesAsync();

        var handler = new GetAllKitchenTasksHandler(db);
        var query = new GetAllKitchenTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        var dto = okResult.Value![0];
        
        Assert.Equal(taskId, dto.Id);
        Assert.Equal(orderId, dto.OrderId);
        Assert.Equal(assignedToId, dto.AssignedTo);
        Assert.Equal("Preparing", dto.Status);
        Assert.Equal("Special preparation notes", dto.Notes);
        Assert.Equal(updatedAt, dto.UpdatedAt);
        Assert.Equal("Preparing", dto.OrderStatus);
    }

    [Fact]
    public async Task GetAllKitchenTasks_Should_Handle_Tasks_With_Null_Notes()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
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
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.NotStarted,
            Notes = null,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new GetAllKitchenTasksHandler(db);
        var query = new GetAllKitchenTasksQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Single(okResult.Value);
        Assert.Null(okResult.Value[0].Notes);
    }
}
