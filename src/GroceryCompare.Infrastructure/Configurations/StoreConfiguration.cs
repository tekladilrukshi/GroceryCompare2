using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.Suburb).HasMaxLength(100);
        builder.Property(s => s.Region).HasMaxLength(100);
        builder.Property(s => s.ExternalStoreCode).HasMaxLength(50);

        builder.HasOne(s => s.Franchise)
            .WithMany(f => f.Stores)
            .HasForeignKey(s => s.FranchiseId)
            .OnDelete(DeleteBehavior.Restrict);

        // The store-sync upsert (PBI-015) matches on this pair.
        builder.HasIndex(s => new { s.FranchiseId, s.ExternalStoreCode }).IsUnique();
    }
}
