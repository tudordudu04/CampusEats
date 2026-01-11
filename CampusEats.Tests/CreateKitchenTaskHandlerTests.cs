using Xunit;
using CampusEats.Api.Features.Kitchen.CreateKitchenTask;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using FluentValidation;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class CreateKitchenTaskHandlerTests
{
    [Fact]
    public async Task CreateKitchenTask_Should_Create_Task_Successfully()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(orderId, assignedToId, "Prepare pizza");

        var result = await handler.Handle(command, CancellationToken.None);

        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<KitchenTask>>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(orderId, createdResult.Value.OrderId);
        Assert.Equal(assignedToId, createdResult.Value.AssignedTo);
        Assert.Equal("Prepare pizza", createdResult.Value.Notes);
        Assert.Equal(KitchenTaskStatus.NotStarted, createdResult.Value.Status);

        var taskInDb = await db.KitchenTasks.FirstOrDefaultAsync(t => t.OrderId == orderId);
        Assert.NotNull(taskInDb);
        Assert.Equal(assignedToId, taskInDb.AssignedTo);
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Log_Information_After_Creation()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(orderId, assignedToId, null);

        await handler.Handle(command, CancellationToken.None);

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<Microsoft.Extensions.Logging.EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Created KitchenTask") && o.ToString()!.Contains(orderId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Return_Created_Result_With_Correct_Location()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Preparing,
            Total = 75m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(orderId, assignedToId, "Special instructions");

        var result = await handler.Handle(command, CancellationToken.None);

        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<KitchenTask>>(result);
        Assert.NotNull(createdResult.Location);
        Assert.Contains("/api/kitchen/tasks/", createdResult.Location);
        Assert.Contains(createdResult.Value!.Id.ToString(), createdResult.Location);
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Throw_ValidationException_When_OrderId_Is_Empty()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(Guid.Empty, Guid.NewGuid(), "Notes");

        await Assert.ThrowsAsync<ValidationException>(() => 
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Throw_ValidationException_When_AssignedTo_Is_Empty()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(Guid.NewGuid(), Guid.Empty, "Notes");

        await Assert.ThrowsAsync<ValidationException>(() => 
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Throw_ValidationException_When_Notes_Exceed_Maximum_Length()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var longNotes = new string('A', 101);
        var command = new CreateKitchenTaskCommand(Guid.NewGuid(), Guid.NewGuid(), longNotes);

        await Assert.ThrowsAsync<ValidationException>(() => 
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Create_Task_With_Null_Notes()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 30m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(orderId, assignedToId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<KitchenTask>>(result);
        Assert.Null(createdResult.Value!.Notes);
    }

    [Fact]
    public async Task CreateKitchenTask_Should_Set_Status_To_NotStarted()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedToId = Guid.NewGuid();

        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pending,
            Total = 60m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CreateKitchenTaskValidator();
        var logger = Substitute.For<ILogger<CreateKitchenTaskHandler>>();
        var handler = new CreateKitchenTaskHandler(db, validator, logger);
        
        var command = new CreateKitchenTaskCommand(orderId, assignedToId, "Test task");

        var result = await handler.Handle(command, CancellationToken.None);

        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<KitchenTask>>(result);
        Assert.Equal(KitchenTaskStatus.NotStarted, createdResult.Value!.Status);
    }
}
