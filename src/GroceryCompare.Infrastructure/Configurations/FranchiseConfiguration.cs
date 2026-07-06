using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class FranchiseConfiguration : IEntityTypeConfiguration<Franchise>
{
    public void Configure(EntityTypeBuilder<Franchise> builder)
    {
        builder.Property(f => f.Name).HasMaxLength(100);
        builder.HasIndex(f => f.Name).IsUnique();

        // Fixed IDs so later code/seed data can reference franchises reliably.
        // HasData seeding is migration-based and idempotent (PBI-011).
        builder.HasData(
            new Franchise { Id = 1, Name = "Pak'nSave" },
            new Franchise { Id = 2, Name = "Woolworths NZ" },
            new Franchise { Id = 3, Name = "New World" });
    }
}
