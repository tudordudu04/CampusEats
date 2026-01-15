using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Auth.Logout;
using CampusEats.Api.Features.Auth.Refresh;
using CampusEats.Api.Features.Auth.GetUser;
using CampusEats.Api.Features.Auth.GetAllUsers;
using CampusEats.Api.Features.Auth.DeleteUser;
using CampusEats.Api.Features.Auth.UpdateUserProfile;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using System.Security.Claims;
using CampusEats.Tests;

namespace CampusEats.Tests.AuthTests;

public class AuthTest
{
    [Fact]
    public async Task RegisterUser_Should_Create_User_And_LoyaltyAccount()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var passwordService = Substitute.For<IPasswordService>(); 
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        passwordService.Hash(Arg.Any<User>(), Arg.Any<string>())
            .Returns("hashed_pass");
        
        jwtService.GenerateRefreshToken().Returns(("token","hash",DateTime.UtcNow.AddDays(7)));
        jwtService.GenerateAccessToken(Arg.Any<User>()).Returns("jwt_access_token");
        
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
        
        var handler = new RegisterUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new RegisterUserCommand("Student Test", "student@gmail.com", "Pass123!", UserRole.STUDENT);
        
        await handler.Handle(command, CancellationToken.None);
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email =="student@gmail.com" );
        Assert.NotNull(user);
        Assert.Equal("hashed_pass", user.PasswordHash);
        
        var loyalty = await db.LoyaltyAccounts.FirstOrDefaultAsync(l => l.UserId == user.Id);
        Assert.NotNull(loyalty);
        Assert.Equal(0, loyalty.Points);
    }

    [Fact]
    public async Task Login_Should_Return_AccessToken_And_Set_RefreshToken_Cookie()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var passwordService = Substitute.For<IPasswordService>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        passwordService.Verify(Arg.Any<User>(), "hashed_password", "correct_password").Returns(true);
        jwtService.GenerateRefreshToken().Returns(("refresh_token", "refresh_hash", DateTime.UtcNow.AddDays(7)));
        jwtService.GenerateAccessToken(Arg.Any<User>()).Returns("access_token");
        
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var handler = new LoginUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new LoginUserCommand("test@example.com", "correct_password");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsType<Ok<AuthResultDto>>(result);
        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        Assert.NotNull(refreshToken);
        Assert.Equal("refresh_hash", refreshToken.TokenHash);
        Assert.Null(refreshToken.RevokedAtUtc);
    }

    [Fact]
    public async Task Login_Should_Fail_With_Invalid_Credentials()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var passwordService = Substitute.For<IPasswordService>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        passwordService.Verify(Arg.Any<User>(), "hashed_password", "wrong_password").Returns(false);
        
        var handler = new LoginUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new LoginUserCommand("test@example.com", "wrong_password");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsType<BadRequest<string>>(result);
    }

    [Fact]
    public async Task GetUser_Should_Return_UserDto()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetUserHandler(db);
        var query = new GetUserQuery(userId);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetAllUsers_Should_Return_All_Users()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@test.com", PasswordHash = "hash", Role = UserRole.STUDENT, CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@test.com", PasswordHash = "hash", Role = UserRole.WORKER, CreatedAtUtc = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Name = "User 3", Email = "user3@test.com", PasswordHash = "hash", Role = UserRole.MANAGER, CreatedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.MANAGER.ToString())
        }, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetAllUsersHandler(db, httpContextAccessor);
        var query = new GetAllUsersQuery();
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.IsType<Ok<List<UserDto>>>(result);
        var okResult = (Ok<List<UserDto>>)result;
        Assert.Equal(3, okResult.Value.Count);
    }

    [Fact]
    public async Task DeleteUser_Should_Remove_User_And_RefreshTokens()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Name = "User to Delete",
            Email = "delete@test.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = "token_hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, managerId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.MANAGER.ToString())
        }, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var handler = new DeleteUserHandler(db, httpContextAccessor);
        var command = new DeleteUserCommand(userId);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert - verific�m c� e un r�spuns Ok, f�r� s� specific�m tipul exact
        Assert.NotNull(result);
        var deletedUser = await db.Users.FindAsync(userId);
        Assert.Null(deletedUser);
        var tokens = await db.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        Assert.Empty(tokens);
    }

    [Fact]
    public async Task UpdateUserProfile_Should_Update_Name_And_Email()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "old@test.com",
            PasswordHash = "hash",
            Role = UserRole.STUDENT,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var handler = new UpdateUserProfileHandler(db, httpContextAccessor);
        var command = new UpdateUserProfileCommand
        {
            Name = "New Name"
        };
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert - verific�m c� e un r�spuns Ok, f�r� s� specific�m tipul exact
        Assert.NotNull(result);
        var updatedUser = await db.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal("New Name", updatedUser.Name);
    }
    [Fact]
    public async Task Login_Should_Return_BadRequest_When_User_Does_Not_Exist()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var passwordService = Substitute.For<IPasswordService>();
        var handler = new LoginUserHandler(db, passwordService, Substitute.For<IJwtTokenService>(), Substitute.For<IHttpContextAccessor>());
    
        var command = new LoginUserCommand("nonexistent@test.com", "any_pass");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequest<string>>(result);
    }

    [Fact]
    public async Task Login_Should_Revoke_Existing_Tokens()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var user = new User { Id = userId,Name = "Test Name", Email = "test@test.com", PasswordHash = "hash" };
        db.Users.Add(user);
    
        var oldToken = new RefreshToken { UserId = userId, TokenHash = "old_hash", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        db.RefreshTokens.Add(oldToken);
        await db.SaveChangesAsync();

        var passwordService = Substitute.For<IPasswordService>();
        passwordService.Verify(user, "hash", "pass").Returns(true);
    
        var jwtService = Substitute.For<IJwtTokenService>();
        jwtService.GenerateRefreshToken().Returns(("new_rt", "new_hash", DateTime.UtcNow.AddDays(7)));

        var handler = new LoginUserHandler(db, passwordService, jwtService, SetupUserContext(userId));
    
        // Act
        await handler.Handle(new LoginUserCommand("test@test.com", "pass"), CancellationToken.None);

        // Assert
        var tokenFromDb = await db.RefreshTokens.FirstAsync(t => t.TokenHash == "old_hash");
        Assert.NotNull(tokenFromDb.RevokedAtUtc); // Verific�m c� jetonul vechi a fost revocat
    }
    private IHttpContextAccessor SetupUserContext(Guid userId)
    {
        var http = Substitute.For<IHttpContextAccessor>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth"));
    
        var httpContext = new DefaultHttpContext();
        httpContext.User = user;
        http.HttpContext.Returns(httpContext);
    
        return http;
    }
    
    

    [Fact]
    public async Task Logout_Should_Return_NoContent_When_Cookie_Is_Missing()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var httpContext = Substitute.For<IHttpContextAccessor>();
        httpContext.HttpContext.Returns(new DefaultHttpContext()); // Request f�r� cookie-uri
    
        var handler = new LogoutHandler(db, Substitute.For<IJwtTokenService>(), httpContext);
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);
    
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NoContent>(result);
    }

    [Fact]
    public async Task Refresh_Should_Return_Unauthorized_When_Token_Is_Expired()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        var jwt = Substitute.For<IJwtTokenService>();
        jwt.Hash("expired_token").Returns("hashed");
    
        db.RefreshTokens.Add(new RefreshToken { TokenHash = "hashed", ExpiresAtUtc = DateTime.UtcNow.AddHours(-1) });
        await db.SaveChangesAsync();
    
        var http = Substitute.For<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();
        ctx.Request.Cookies = CreateCookieCollection("refresh_token", "expired_token");
        http.HttpContext.Returns(ctx);

        var handler = new RefreshHandler(db, jwt, http);
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);
    
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task RegisterUser_Should_Return_Unauthorized_When_NonStudent_Without_Auth()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var passwordService = Substitute.For<IPasswordService>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        // HttpContext is null (no authentication)
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        
        var handler = new RegisterUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new RegisterUserCommand("Worker Test", "Worker@test.com", "Pass123!", UserRole.WORKER);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task RegisterUser_Should_Return_Forbid_When_NonManager_Tries_To_Register_Worker()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var passwordService = Substitute.For<IPasswordService>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        // Create authenticated context with STUDENT role (not MANAGER)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, UserRole.STUDENT.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var handler = new RegisterUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new RegisterUserCommand("Worker Test", "Worker@test.com", "Pass123!", UserRole.WORKER);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsType<ForbidHttpResult>(result);
    }

    [Fact]
    public async Task RegisterUser_Should_Return_Ok_When_Manager_Creates_Worker()
    {
        // Arrange
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var passwordService = Substitute.For<IPasswordService>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
        passwordService.Hash(Arg.Any<User>(), Arg.Any<string>()).Returns("hashed_pass");
        jwtService.GenerateAccessToken(Arg.Any<User>()).Returns("jwt_token");
        
        // Create authenticated context with MANAGER role
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, UserRole.MANAGER.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var handler = new RegisterUserHandler(db, passwordService, jwtService, httpContextAccessor);
        var command = new RegisterUserCommand("Worker Test", "Worker@test.com", "Pass123!", UserRole.WORKER);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsType<Ok<AuthResultDto>>(result);
        var okResult = (Ok<AuthResultDto>)result;
        Assert.NotNull(okResult.Value);
        Assert.NotNull(okResult.Value.AccessToken);
    }

    private IRequestCookieCollection CreateCookieCollection(string key, string value) {
        var mock = Substitute.For<IRequestCookieCollection>();
        mock.TryGetValue(key, out Arg.Any<string>()).Returns(x => { x[1] = value; return true; });
        return mock;
    }
}
