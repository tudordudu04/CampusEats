using System.Security.Claims;
using CampusEats.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace CampusEats.Tests;

public static class TestDbHelper
{
    public static AppDbContext GetInMemoryDbContext()
    {
        //TODO Aici trebuie cu un Guid deja generat
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
    
    public static IHttpContextAccessor SetupUserContext(Guid userId, string role = "MANAGER")
    {
        var http = Substitute.For<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        
        http.HttpContext.Returns(new DefaultHttpContext { User = user });
        return http;
    }
}