using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.HasKey(i => new { i.ShoppingListId, i.GroceryItemId });

        builder.HasOne(i => i.ShoppingList)
            .WithMany(l => l.Items)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.GroceryItem)
            .WithMany()
            .HasForeignKey(i => i.GroceryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
