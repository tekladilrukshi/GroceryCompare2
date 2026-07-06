using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryCompare.Infrastructure;

public class GroceryCompareDbContext(DbContextOptions<GroceryCompareDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Franchise> Franchises => Set<Franchise>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<UserStoreSelection> UserStoreSelections => Set<UserStoreSelection>();
    public DbSet<GroceryItem> GroceryItems => Set<GroceryItem>();
    public DbSet<ItemAlias> ItemAliases => Set<ItemAlias>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ScrapeRun> ScrapeRuns => Set<ScrapeRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Per-entity configuration classes live in Configurations/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GroceryCompareDbContext).Assembly);
    }
}
