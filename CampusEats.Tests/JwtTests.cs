using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampusEats.Tests;

public class JwtTests
{
    [Fact]
    public void GenerateAccessToken_Should_Return_Valid_String()
    {
        // 1. Creăm obiectul JwtOptions direct (fără Options.Create)
        var options = new JwtOptions 
        { 
            SigningKey = "o_cheie_secreta_foarte_lunga_pentru_testare_123456", 
            Issuer = "CampusEats",
            Audience = "CampusEatsUsers",
            AccessTokenMinutes = 60
        };
    
        // 2. Pasăm obiectul direct constructorului
        var service = new JwtTokenService(options);
    
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@campuseats.com", 
            Role = UserRole.STUDENT, 
            Name = "Test User" 
        };

        var token = service.GenerateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}