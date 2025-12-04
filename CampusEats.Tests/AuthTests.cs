using Xunit;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using CampusEats.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using NSubstitute;
namespace CampusEats.Tests;

public class AuthTests
{
    [Fact]
    public async Task RegisterUser_Should_Create_User_And_LoyaltyAccount()
    {
        using var db = TestDbHelper.GetInMemoryDbContext();
        
        var passwordService = Substitute.For<IPasswordService>(); 
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContextAccesor = Substitute.For<IHttpContextAccessor>();
        
        passwordService.Hash(Arg.Any<User>(), Arg.Any<string>())
            .Returns("hashed_pass");
        
        jwtService.GenerateRefreshToken().Returns(("token","hash",DateTime.Now.AddDays(7)));
        jwtService.GenerateAccessToken(Arg.Any<User>()).Returns("jwt_access_token");
        
        httpContextAccesor.HttpContext.Returns(new DefaultHttpContext());
        
        var handler = new RegisterUserHandler(db, passwordService, jwtService, httpContextAccesor);
        var command = new RegisterUserCommand("Student Test", "student@gmail.com", "Pass123!", UserRole.STUDENT);
        
        await handler.Handle(command, CancellationToken.None);
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email =="student@gmail.com" );
        Assert.NotNull(user);
        Assert.Equal("hashed_pass", user.PasswordHash);
        
        var loyalty = await db.LoyaltyAccounts.FirstOrDefaultAsync(l => l.UserId == user.Id);
        Assert.NotNull(loyalty);
        Assert.Equal(0, loyalty.Points);
    }
}