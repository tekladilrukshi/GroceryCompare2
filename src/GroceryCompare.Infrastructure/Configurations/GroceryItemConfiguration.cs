using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class GroceryItemConfiguration : IEntityTypeConfiguration<GroceryItem>
{
    public void Configure(EntityTypeBuilder<GroceryItem> builder)
    {
        builder.Property(i => i.CanonicalName).HasMaxLength(200);
        builder.Property(i => i.Brand).HasMaxLength(100);
        builder.Property(i => i.Size).HasMaxLength(50);
        builder.Property(i => i.Barcode).HasMaxLength(32);
        builder.Property(i => i.Category).HasMaxLength(100);

        // Plain B-tree index serves the MVP LIKE search (PBI-022);
        // full-text/trigram upgrade is Phase 3 (PBI-041).
        builder.HasIndex(i => i.CanonicalName);
    }
}
