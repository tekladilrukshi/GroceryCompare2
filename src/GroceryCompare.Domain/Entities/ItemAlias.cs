namespace GroceryCompare.Domain.Entities;

/// <summary>Maps a canonical <see cref="GroceryItem"/> to one retailer's own
/// SKU. Scoped to a franchise (codes shared chain-wide) or a single store —
/// at least one of the two must be set (enforced by a check constraint).</summary>
public class ItemAlias
{
    public int Id { get; set; }

    public int GroceryItemId { get; set; }

    public GroceryItem? GroceryItem { get; set; }

    public int? FranchiseId { get; set; }

    public Franchise? Franchise { get; set; }

    public int? StoreId { get; set; }

    public Store? Store { get; set; }

    public required string ExternalProductCode { get; set; }

    public required string ExternalName { get; set; }

    public ICollection<Price> Prices { get; set; } = [];
}
