using Xunit;
using CampusEats.Api.Features.Reviews.AddReview;
using CampusEats.Api.Features.Reviews.UpdateReview;
using CampusEats.Api.Features.Reviews.DeleteReview;
using CampusEats.Api.Features.Reviews.GetMenuItemReviews;
using CampusEats.Api.Features.Reviews.GetUserReview;
using CampusEats.Api.Features.Reviews.GetMenuItemRating;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class ReviewsTests
{
    [Fact]
    public async Task AddReview_Should_Create_Review_Successfully()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza Margherita",
            Price: 25.99m,
            Description: "Delicious pizza",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: new[] { "Gluten", "Dairy" }
        );
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new AddReviewHandler(db);
        var command = new AddReviewCommand(
            MenuItemId: menuItem.Id,
            UserId: user.Id,
            Rating: 4.5m,
            Comment: "Great pizza!"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(menuItem.Id, result.MenuItemId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(4.5m, result.Rating);
        Assert.Equal("Great pizza!", result.Comment);
        
        var reviewInDb = await db.MenuItemReviews.FindAsync(result.Id);
        Assert.NotNull(reviewInDb);
        Assert.Equal(4.5m, reviewInDb.Rating);
    }

    [Fact]
    public async Task AddReview_Should_Throw_When_User_Already_Has_Review()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var existingReview = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 3.0m,
            Comment = "Old review",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(existingReview);
        await db.SaveChangesAsync();
        
        var handler = new AddReviewHandler(db);
        var command = new AddReviewCommand(menuItem.Id, user.Id, 4.5m, "New review");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task AddReview_Should_Throw_When_Rating_Out_Of_Range()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new AddReviewHandler(db);
        var command = new AddReviewCommand(menuItem.Id, user.Id, 6.0m, "Invalid rating");
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateReview_Should_Update_Existing_Review()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 3.0m,
            Comment = "Old comment",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new UpdateReviewHandler(db);
        var command = new UpdateReviewCommand(
            ReviewId: review.Id,
            UserId: user.Id,
            Rating: 5.0m,
            Comment: "Updated comment - much better now!"
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.0m, result.Rating);
        Assert.Equal("Updated comment - much better now!", result.Comment);
        
        var updatedReview = await db.MenuItemReviews.FindAsync(review.Id);
        Assert.NotNull(updatedReview);
        Assert.Equal(5.0m, updatedReview.Rating);
        Assert.True(updatedReview.UpdatedAtUtc > updatedReview.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateReview_Should_Throw_When_User_Not_Owner()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Name = "Owner",
            Email = "owner@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Other User",
            Email = "other@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = owner.Id,
            Rating = 3.0m,
            Comment = "Owner's review",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(owner);
        await db.Users.AddAsync(otherUser);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new UpdateReviewHandler(db);
        var command = new UpdateReviewCommand(review.Id, otherUser.Id, 5.0m, "Trying to update someone else's review");
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteReview_Should_Remove_Review()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 3.0m,
            Comment = "To be deleted",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new DeleteReviewHandler(db);
        var command = new DeleteReviewCommand(review.Id, user.Id);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        var deletedReview = await db.MenuItemReviews.FindAsync(review.Id);
        Assert.Null(deletedReview);
    }

    [Fact]
    public async Task GetMenuItemReviews_Should_Return_All_Reviews_For_Item()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User One",
            Email = "user1@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User Two",
            Email = "user2@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review1 = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user1.Id,
            Rating = 4.0m,
            Comment = "Good!",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        
        var review2 = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user2.Id,
            Rating = 5.0m,
            Comment = "Excellent!",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        
        await db.Users.AddAsync(user1);
        await db.Users.AddAsync(user2);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review1);
        await db.MenuItemReviews.AddAsync(review2);
        await db.SaveChangesAsync();
        
        var handler = new GetMenuItemReviewsHandler(db);
        var query = new GetMenuItemReviewsQuery(menuItem.Id);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Rating == 4.0m && r.Comment == "Good!");
        Assert.Contains(result, r => r.Rating == 5.0m && r.Comment == "Excellent!");
    }

    [Fact]
    public async Task GetUserReview_Should_Return_Users_Review()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 4.5m,
            Comment = "My review",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new GetUserReviewHandler(db);
        var query = new GetUserReviewQuery(menuItem.Id, user.Id);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(4.5m, result.Rating);
        Assert.Equal("My review", result.Comment);
    }

    [Fact]
    public async Task GetUserReview_Should_Return_Null_When_No_Review()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new GetUserReviewHandler(db);
        var query = new GetUserReviewQuery(menuItem.Id, user.Id);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMenuItemRating_Should_Calculate_Average_Correctly()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User One",
            Email = "user1@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User Two",
            Email = "user2@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var user3 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User Three",
            Email = "user3@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var reviews = new[]
        {
            new MenuItemReview
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                UserId = user1.Id,
                Rating = 3.0m,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new MenuItemReview
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                UserId = user2.Id,
                Rating = 4.0m,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new MenuItemReview
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                UserId = user3.Id,
                Rating = 5.0m,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
        
        await db.Users.AddAsync(user1);
        await db.Users.AddAsync(user2);
        await db.Users.AddAsync(user3);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddRangeAsync(reviews);
        await db.SaveChangesAsync();
        
        var handler = new GetMenuItemRatingHandler(db);
        var query = new GetMenuItemRatingQuery(menuItem.Id);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(4.0m, result.AverageRating); // (3 + 4 + 5) / 3 = 4.0
        Assert.Equal(3, result.TotalReviews);
    }

    [Fact]
    public async Task GetMenuItemRating_Should_Return_Zero_When_No_Reviews()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new GetMenuItemRatingHandler(db);
        var query = new GetMenuItemRatingQuery(menuItem.Id);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.AverageRating);
        Assert.Equal(0, result.TotalReviews);
    }

    [Fact]
    public async Task Review_Should_Support_Half_Star_Ratings()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.SaveChangesAsync();
        
        var handler = new AddReviewHandler(db);
        var command = new AddReviewCommand(menuItem.Id, user.Id, 3.5m, "Half star test");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.Equal(3.5m, result.Rating);
        
        var review = await db.MenuItemReviews.FindAsync(result.Id);
        Assert.NotNull(review);
        Assert.Equal(3.5m, review.Rating);
    }

    [Fact]
    public async Task AddReview_Should_Throw_When_MenuItem_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        
        var handler = new AddReviewHandler(db);
        var nonExistentMenuItemId = Guid.NewGuid();
        var command = new AddReviewCommand(nonExistentMenuItemId, user.Id, 4.0m, "This should fail");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateReview_Should_Throw_When_Review_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var nonExistentReviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var handler = new UpdateReviewHandler(db);
        var command = new UpdateReviewCommand(nonExistentReviewId, userId, 5.0m, "This should fail");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateReview_Should_Throw_When_Rating_Too_Low()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 3.0m,
            Comment = "Original",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new UpdateReviewHandler(db);
        var command = new UpdateReviewCommand(review.Id, user.Id, 0.5m, "Too low rating");
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateReview_Should_Throw_When_Rating_Too_High()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = user.Id,
            Rating = 3.0m,
            Comment = "Original",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(user);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new UpdateReviewHandler(db);
        var command = new UpdateReviewCommand(review.Id, user.Id, 5.5m, "Too high rating");
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteReview_Should_Throw_When_Review_Not_Found()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var nonExistentReviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var handler = new DeleteReviewHandler(db);
        var command = new DeleteReviewCommand(nonExistentReviewId, userId);
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteReview_Should_Throw_When_User_Not_Owner()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Name = "Owner",
            Email = "owner@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Other User",
            Email = "other@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        var menuItem = new MenuItem(
            Id: Guid.NewGuid(),
            Name: "Pizza",
            Price: 25.99m,
            Description: "Test",
            Category: MenuCategory.PIZZA,
            ImageUrl: null,
            Allergens: Array.Empty<string>()
        );
        
        var review = new MenuItemReview
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            UserId = owner.Id,
            Rating = 3.0m,
            Comment = "Owner's review",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await db.Users.AddAsync(owner);
        await db.Users.AddAsync(otherUser);
        await db.MenuItems.AddAsync(menuItem);
        await db.MenuItemReviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        var handler = new DeleteReviewHandler(db);
        var command = new DeleteReviewCommand(review.Id, otherUser.Id);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.Handle(command, CancellationToken.None)
        );
    }
}
