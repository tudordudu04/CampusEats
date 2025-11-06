using Microsoft.EntityFrameworkCore;
using CampusEats.Api.Domain;

namespace CampusEats.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
}