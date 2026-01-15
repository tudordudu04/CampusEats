using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.UpdateUserProfile;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusEats.Tests.AuthTests;

public class UpdateUserProfileTests
{
    private static ClaimsPrincipal CreatePrincipal(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "STUDENT")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task Handle_Should_Update_Fields_And_Return_Ok_When_User_Exists()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        // Seed user
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "test@example.com",
            Role = UserRole.STUDENT,
            AddressCity = "Old City",
            PasswordHash = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var context = new DefaultHttpContext { User = CreatePrincipal(userId) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new UpdateUserProfileHandler(db, http);

        var command = new UpdateUserProfileCommand
        {
            Name = "New Name",
            AddressCity = "New City",
            AddressStreet = "New Street"
            // Other fields null
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var okResult = result as IValueHttpResult; // Generic Ok check for anonymous type
        Assert.NotNull(okResult);
        Assert.Equal(200, (result as IStatusCodeHttpResult)?.StatusCode);

        // Verify DB
        var updatedUser = await db.Users.FindAsync(userId);
        Assert.Equal("New Name", updatedUser!.Name);
        Assert.Equal("New City", updatedUser.AddressCity);
        Assert.Equal("New Street", updatedUser.AddressStreet); // Updated
        Assert.Equal("test@example.com", updatedUser.Email); // Unchanged
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }; // No claims
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new UpdateUserProfileHandler(db, http);

        var command = new UpdateUserProfileCommand { Name = "New Name" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_In_Token_Does_Not_Exist_In_Db()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        // We do strictly NOT add the user to the DB

        var context = new DefaultHttpContext { User = CreatePrincipal(userId) };
        var http = new HttpContextAccessor { HttpContext = context };
        var handler = new UpdateUserProfileHandler(db, http);

        var command = new UpdateUserProfileCommand { Name = "Ghost" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result);
    }
}

public class UpdateUserProfileEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UpdateUserProfileEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_UpdateProfile"));
            });
        });
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Put_Profile_Should_Return_Ok_And_Update_Data()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Seed User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Integration Initial",
            Email = "update@test.com",
            Role = UserRole.STUDENT,
            PasswordHash = "x",
            AddressCity = "Initial City"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 2. Generate Token
        var token = jwtService.GenerateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 3. Prepare Request
        var command = new UpdateUserProfileCommand
        {
            Name = "Integration Updated",
            AddressCity = "Updated City"
        };

        // 4. Act
        var response = await client.PutAsJsonAsync("/auth/profile", command);

        // 5. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify JSON response reflects changes
        var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(responseData);
        Assert.Equal("Integration Updated", responseData["name"].ToString());
        Assert.Equal("Updated City", responseData["addressCity"].ToString());

        // Verify DB persistence
        using var checkScope = _factory.Services.CreateScope();
        var checkDb = CreateDbContext(checkScope);
        var dbUser = await checkDb.Users.FindAsync(user.Id);
        Assert.Equal("Integration Updated", dbUser!.Name);
        Assert.Equal("Updated City", dbUser.AddressCity);
    }

    [Fact]
    public async Task Put_Profile_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        var command = new UpdateUserProfileCommand { Name = "Hacker" };

        var response = await client.PutAsJsonAsync("/auth/profile", command);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
