using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class ItemAliasConfiguration : IEntityTypeConfiguration<ItemAlias>
{
    public void Configure(EntityTypeBuilder<ItemAlias> builder)
    {
        builder.Property(a => a.ExternalProductCode).HasMaxLength(64);
        builder.Property(a => a.ExternalName).HasMaxLength(300);

        builder.HasOne(a => a.GroceryItem)
            .WithMany(i => i.Aliases)
            .HasForeignKey(a => a.GroceryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Franchise)
            .WithMany()
            .HasForeignKey(a => a.FranchiseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Store)
            .WithMany()
            .HasForeignKey(a => a.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // An alias is scoped to a franchise (chain-wide product codes) or a
        // single store; a row tied to neither would be meaningless.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ItemAlias_FranchiseOrStore",
            "[FranchiseId] IS NOT NULL OR [StoreId] IS NOT NULL"));

        builder.HasIndex(a => new { a.FranchiseId, a.ExternalProductCode });
        builder.HasIndex(a => a.ExternalName);
    }
}
