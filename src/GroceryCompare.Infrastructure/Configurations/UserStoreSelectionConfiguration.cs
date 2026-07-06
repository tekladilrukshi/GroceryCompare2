using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class UserStoreSelectionConfiguration : IEntityTypeConfiguration<UserStoreSelection>
{
    public void Configure(EntityTypeBuilder<UserStoreSelection> builder)
    {
        builder.HasKey(s => new { s.UserId, s.StoreId });

        builder.HasOne(s => s.User)
            .WithMany(u => u.StoreSelections)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
