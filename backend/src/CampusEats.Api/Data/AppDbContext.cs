using Microsoft.EntityFrameworkCore;
using CampusEats.Api.Domain;

namespace CampusEats.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    
    public DbSet<KitchenTask> KitchenTasks => Set<KitchenTask>();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<User>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    
}
