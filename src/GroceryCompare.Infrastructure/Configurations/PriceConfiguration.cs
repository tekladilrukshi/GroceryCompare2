using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(10, 2);

        builder.HasOne(p => p.ItemAlias)
            .WithMany(a => a.Prices)
            .HasForeignKey(p => p.ItemAliasId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict avoids SQL Server multiple-cascade-path errors
        // (Store -> ItemAlias -> Price would conflict with Store -> Price).
        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // "Current price per item per store" is the hottest query in the app
        // (architecture-plan.md §4).
        builder.HasIndex(p => new { p.ItemAliasId, p.StoreId, p.CapturedAt })
            .IsDescending(false, false, true);
    }
}
