using CampusEats.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
    
}