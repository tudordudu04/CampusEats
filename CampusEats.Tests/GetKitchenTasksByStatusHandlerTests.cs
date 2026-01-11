using Xunit;
using CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;
using CampusEats.Api.Features.Kitchen;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class GetKitchenTasksByStatusHandlerTests
{
    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Return_BadRequest_When_Status_Is_Invalid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("InvalidStatus");

        var result = await handler.Handle(query, CancellationToken.None);

        var badRequestResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result);
        Assert.Contains("Invalid status", badRequestResult.Value);
        Assert.Contains("InvalidStatus", badRequestResult.Value);
    }

    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Return_Empty_List_When_No_Tasks_Match_Status()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.Add(new KitchenTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.NotStarted,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("Preparing");

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Empty(okResult.Value);
    }

    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Return_Tasks_Matching_Status()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var orderId3 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.AddRange(
            new Order { Id = orderId1, UserId = userId, Status = OrderStatus.Pending, Total = 50m, CreatedAt = DateTime.UtcNow },
            new Order { Id = orderId2, UserId = userId, Status = OrderStatus.Preparing, Total = 75m, CreatedAt = DateTime.UtcNow },
            new Order { Id = orderId3, UserId = userId, Status = OrderStatus.Preparing, Total = 60m, CreatedAt = DateTime.UtcNow }
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
            Status = KitchenTaskStatus.Preparing,
            Notes = "Task 3",
            UpdatedAt = DateTime.UtcNow
        };

        db.KitchenTasks.AddRange(task1, task2, task3);
        await db.SaveChangesAsync();

        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("Preparing");

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
        Assert.All(okResult.Value, dto => Assert.Equal("Preparing", dto.Status));
        Assert.Contains(okResult.Value, dto => dto.Id == task2.Id);
        Assert.Contains(okResult.Value, dto => dto.Id == task3.Id);
    }

    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Include_OrderStatus_In_DTO()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Completed,
            Total = 120m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            AssignedTo = assignedToId,
            Status = KitchenTaskStatus.Completed,
            Notes = "Finished",
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("Completed");

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.Single(okResult.Value!);
        
        var dto = okResult.Value![0];
        Assert.Equal(taskId, dto.Id);
        Assert.Equal(orderId, dto.OrderId);
        Assert.Equal(assignedToId, dto.AssignedTo);
        Assert.Equal("Completed", dto.Status);
        Assert.Equal("Finished", dto.Notes);
        Assert.Equal("Completed", dto.OrderStatus);
    }

    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Be_Case_Insensitive_For_Status()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 40m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.Add(new KitchenTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AssignedTo = Guid.NewGuid(),
            Status = KitchenTaskStatus.NotStarted,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("notstarted");

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task GetKitchenTasksByStatus_Should_Return_All_Task_Properties_Correctly()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 1, 11, 10, 30, 0);

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Preparing,
            Total = 85m,
            CreatedAt = DateTime.UtcNow
        });

        db.KitchenTasks.Add(new KitchenTask
        {
            Id = taskId,
            OrderId = orderId,
            AssignedTo = assignedToId,
            Status = KitchenTaskStatus.Preparing,
            Notes = "Special instructions",
            UpdatedAt = updatedAt
        });

        await db.SaveChangesAsync();

        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery("Preparing");

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = okResult.Value[0];
        
        Assert.Equal(taskId, dto.Id);
        Assert.Equal(orderId, dto.OrderId);
        Assert.Equal(assignedToId, dto.AssignedTo);
        Assert.Equal("Preparing", dto.Status);
        Assert.Equal("Special instructions", dto.Notes);
        Assert.Equal(updatedAt, dto.UpdatedAt);
        Assert.Equal("Preparing", dto.OrderStatus);
    }

    [Theory]
    [InlineData("NotStarted")]
    [InlineData("Preparing")]
    [InlineData("Completed")]
    public async Task GetKitchenTasksByStatus_Should_Accept_All_Valid_Statuses(string status)
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var handler = new GetKitchenTasksByStatusHandler(db);
        var query = new GetKitchenTasksByStatusQuery(status);

        var result = await handler.Handle(query, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<KitchenTaskDto>>>(result);
        Assert.NotNull(okResult.Value);
    }
}
