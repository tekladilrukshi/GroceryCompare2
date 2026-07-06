using GroceryCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryCompare.Infrastructure.Configurations;

public class ScrapeRunConfiguration : IEntityTypeConfiguration<ScrapeRun>
{
    public void Configure(EntityTypeBuilder<ScrapeRun> builder)
    {
        // Stored as text so the ops log is readable with plain SQL.
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ErrorSummary).HasMaxLength(2000);

        builder.HasOne(r => r.Franchise)
            .WithMany()
            .HasForeignKey(r => r.FranchiseId)
            .OnDelete(DeleteBehavior.Restrict);

        // "Latest runs per franchise" is how PBI-037 will query this.
        builder.HasIndex(r => new { r.FranchiseId, r.StartedAt }).IsDescending(false, true);
    }
}
