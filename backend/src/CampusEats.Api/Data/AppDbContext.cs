using Microsoft.EntityFrameworkCore;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using Microsoft.AspNetCore.Identity;

namespace CampusEats.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<KitchenTask> KitchenTasks => Set<KitchenTask>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    public async Task EnsureSeedManagerAsync(
        string name,
        string email,
        string password,
        PasswordHasher<User> hasher,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await Users.AnyAsync(u => u.Email == email, cancellationToken))
            return;

        var manager = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Role = UserRole.MANAGER,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        manager.PasswordHash = hasher.HashPassword(manager, password);
        await Users.AddAsync(manager, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.LoyaltyAccount)
            .WithOne(la => la.User)
            .HasForeignKey<LoyaltyAccount>(la => la.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        foreach (var e in ChangeTracker.Entries<User>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        foreach (var e in ChangeTracker.Entries<LoyaltyAccount>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}