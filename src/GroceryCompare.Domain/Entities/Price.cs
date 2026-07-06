namespace GroceryCompare.Domain.Entities;

/// <summary>A time-stamped price capture. Rows are appended, never overwritten,
/// so history is free later (architecture-plan.md §4); everything before
/// Phase 4 only reads the latest row per alias/store.</summary>
public class Price
{
    public long Id { get; set; }

    public int ItemAliasId { get; set; }

    public ItemAlias? ItemAlias { get; set; }

    public int StoreId { get; set; }

    public Store? Store { get; set; }

    public decimal Amount { get; set; }

    public DateTime CapturedAt { get; set; }

    public bool IsOnSale { get; set; }
}
