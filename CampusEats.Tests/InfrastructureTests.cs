using CampusEats.Api.Infrastructure.Security;
using CampusEats.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CampusEats.Tests;

public class InfrastructureTests
{
    [Fact]
    public void PasswordService_Should_Hash_And_Verify_Correctly()
    {
        var hasher = new PasswordHasher<User>();
        var service = new PasswordService(hasher);
        var user = new User { Email = "test@test.com" };
        var password = "SecurePass123!";

        var hash = service.Hash(user, password);
        var isValid = service.Verify(user, hash, password);
        var isInvalid = service.Verify(user, hash, "wrong_pass");

        Assert.True(isValid);
        Assert.False(isInvalid);
    }
}