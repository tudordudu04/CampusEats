using Xunit;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests;

public class ValidatorTests
{
    [Fact]
    public async Task RegisterUserValidator_Should_Fail_When_Email_Exists()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        db.Users.Add(new CampusEats.Api.Domain.User { 
            Name = "Existing", Email = "duplicate@test.com", PasswordHash = "...", Role = UserRole.STUDENT 
        });
        await db.SaveChangesAsync();

        var validator = new RegisterUserValidator(db);
        var command = new RegisterUserCommand("New User", "DUPLICATE@test.com", "Pass123!", UserRole.STUDENT);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Email already registered.");
    }

    [Theory]
    [InlineData("short")] // Prea scurtă
    [InlineData("nouppercase1!")] // Lipsă literă mare
    [InlineData("NOLOWERCASE1!")] // Lipsă literă mică
    [InlineData("NoDigit!")] // Lipsă cifră
    [InlineData("NoSpecialChar1")] // Lipsă caracter special
    public async Task RegisterUserValidator_Should_Fail_On_Weak_Password(string password)
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var validator = new RegisterUserValidator(db);
        var command = new RegisterUserCommand("User", "test@test.com", password, UserRole.STUDENT);

        var result = await validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}