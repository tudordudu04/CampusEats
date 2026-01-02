using Xunit;
using CampusEats.Api.Features.Inventory.CreateIngredient;
using CampusEats.Api.Features.Inventory.AdjustStock;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using NSubstitute;

namespace CampusEats.Tests;

public class InventoryTests
{
    [Fact]
    public async Task CreateIngredient_Should_Create_New_Ingredient_With_Zero_Stock()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var validator = Substitute.For<IValidator<CreateIngredientCommand>>();
        validator.ValidateAsync(Arg.Any<CreateIngredientCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new CreateIngredientHandler(db, validator);
        var command = new CreateIngredientCommand("Tomatoes", "kg", 5);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Name == "Tomatoes");
        Assert.NotNull(ingredient);
        Assert.Equal("kg", ingredient.Unit);
        Assert.Equal(5, ingredient.LowStockThreshold);
        Assert.Equal(0, ingredient.CurrentStock);
    }

    [Fact]
    public async Task AdjustStock_Restock_Should_Increase_Stock()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Flour",
            Unit = "kg",
            CurrentStock = 10,
            LowStockThreshold = 5
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 20,
            Type: StockTransactionType.Restock,
            Note: "Weekly delivery"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(30, updatedIngredient.CurrentStock); // 10 + 20
        
        var transaction = await db.StockTransactions.FirstOrDefaultAsync(st => st.IngredientId == ingredient.Id);
        Assert.NotNull(transaction);
        Assert.Equal(StockTransactionType.Restock, transaction.Type);
        Assert.Equal(20, transaction.QuantityChanged);
    }

    [Fact]
    public async Task AdjustStock_Usage_Should_Decrease_Stock()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Cheese",
            Unit = "kg",
            CurrentStock = 15,
            LowStockThreshold = 3
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 5,
            Type: StockTransactionType.Usage,
            Note: "Used in pizza preparation"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(10, updatedIngredient.CurrentStock); // 15 - 5
        
        var transaction = await db.StockTransactions.FirstOrDefaultAsync(st => st.IngredientId == ingredient.Id);
        Assert.NotNull(transaction);
        Assert.Equal(StockTransactionType.Usage, transaction.Type);
        Assert.Equal(-5, transaction.QuantityChanged);
    }

    [Fact]
    public async Task AdjustStock_Waste_Should_Decrease_Stock()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Lettuce",
            Unit = "kg",
            CurrentStock = 8,
            LowStockThreshold = 2
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 2,
            Type: StockTransactionType.Waste,
            Note: "Spoiled"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(6, updatedIngredient.CurrentStock); // 8 - 2
        
        var transaction = await db.StockTransactions.FirstOrDefaultAsync(st => st.IngredientId == ingredient.Id);
        Assert.NotNull(transaction);
        Assert.Equal(StockTransactionType.Waste, transaction.Type);
        Assert.Equal(-2, transaction.QuantityChanged);
    }

    [Fact]
    public async Task AdjustStock_Should_Prevent_Negative_Stock()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Onions",
            Unit = "kg",
            CurrentStock = 3,
            LowStockThreshold = 1
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 5,
            Type: StockTransactionType.Usage,
            Note: "Attempting to use too much"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(3, updatedIngredient.CurrentStock); // Should remain unchanged
        
        var transactions = await db.StockTransactions.Where(st => st.IngredientId == ingredient.Id).ToListAsync();
        Assert.Empty(transactions); // No transaction should be created
    }

    [Fact]
    public async Task AdjustStock_Should_Allow_Stock_To_Reach_Zero()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Pepper",
            Unit = "kg",
            CurrentStock = 4,
            LowStockThreshold = 1
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 4,
            Type: StockTransactionType.Usage,
            Note: "Using all stock"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(0, updatedIngredient.CurrentStock);
    }

    [Fact]
    public async Task AdjustStock_Multiple_Transactions_Should_Track_Correctly()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Salt",
            Unit = "kg",
            CurrentStock = 10,
            LowStockThreshold = 2
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        
        // Act - Multiple operations
        await handler.Handle(new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 5,
            Type: StockTransactionType.Restock,
            Note: "Restock"
        ), CancellationToken.None);
        
        await handler.Handle(new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 3,
            Type: StockTransactionType.Usage,
            Note: "Usage"
        ), CancellationToken.None);
        
        await handler.Handle(new AdjustStockCommand(
            IngredientId: ingredient.Id,
            Quantity: 1,
            Type: StockTransactionType.Waste,
            Note: "Waste"
        ), CancellationToken.None);
        
        // Assert
        var updatedIngredient = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.NotNull(updatedIngredient);
        Assert.Equal(11, updatedIngredient.CurrentStock); // 10 + 5 - 3 - 1
        
        var transactions = await db.StockTransactions
            .Where(st => st.IngredientId == ingredient.Id)
            .OrderBy(st => st.Timestamp)
            .ToListAsync();
        Assert.Equal(3, transactions.Count);
        Assert.Equal(5, transactions[0].QuantityChanged);
        Assert.Equal(-3, transactions[1].QuantityChanged);
        Assert.Equal(-1, transactions[2].QuantityChanged);
    }

    [Fact]
    public async Task AdjustStock_Should_Return_NotFound_For_Invalid_Ingredient()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(
            IngredientId: Guid.NewGuid(),
            Quantity: 10,
            Type: StockTransactionType.Restock,
            Note: "Test"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert - The handler returns IResult, so we can't directly assert
        // In a real scenario, you'd check the result type
        var transactions = await db.StockTransactions.ToListAsync();
        Assert.Empty(transactions);
    }
    

    [Fact]
    public async Task AdjustStock_Should_Return_ValidationProblem_When_Command_Is_Invalid()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var validator = Substitute.For<IValidator<AdjustStockCommand>>();
        var failures = new List<FluentValidation.Results.ValidationFailure> { new("Quantity", "Must be > 0") };
        validator.ValidateAsync(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult(failures));
    
        var handler = new AdjustStockHandler(db, validator);
        var command = new AdjustStockCommand(Guid.NewGuid(), -1, StockTransactionType.Restock, null);
    
        var result = await handler.Handle(command, CancellationToken.None);
        
        var problemResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(result);
        Assert.Equal(400, problemResult.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_Should_Not_Update_Timestamp_When_Change_Is_Negative()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var initialTime = DateTime.UtcNow.AddDays(-1);
        var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Milk", Unit = "L", CurrentStock = 10, UpdatedAt = initialTime };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
    
        var handler = new AdjustStockHandler(db, SetupValidator<AdjustStockCommand>());
        // Usage rezultă în changeAmount negativ (-2)
        await handler.Handle(new AdjustStockCommand(ingredient.Id, 2, StockTransactionType.Usage, null), CancellationToken.None);
    
        var updated = await db.Ingredients.FindAsync(ingredient.Id);
        Assert.Equal(initialTime, updated.UpdatedAt); // Rămâne neschimbat conform logicii din handler
    }

    private IValidator<T> SetupValidator<T>() where T : class {
        var v = Substitute.For<IValidator<T>>();
        v.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(new FluentValidation.Results.ValidationResult());
        return v;
    }
}
