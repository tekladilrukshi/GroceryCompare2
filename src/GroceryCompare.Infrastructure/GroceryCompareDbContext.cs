using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryCompare.Infrastructure;

public class GroceryCompareDbContext(DbContextOptions<GroceryCompareDbContext> options)
    : DbContext(options)
{
    // Remaining entities (stores, items, prices, lists) are added in PBI-010.
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.GoogleSubjectId).HasMaxLength(64);
            user.Property(u => u.Email).HasMaxLength(320);
            user.Property(u => u.DisplayName).HasMaxLength(200);
            user.HasIndex(u => u.GoogleSubjectId).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.Property(t => t.TokenHash).HasMaxLength(64);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
