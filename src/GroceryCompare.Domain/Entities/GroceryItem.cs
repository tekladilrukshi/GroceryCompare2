namespace GroceryCompare.Domain.Entities;

/// <summary>The app's own canonical product catalog entry; retailers' differing
/// names/codes for the same product map to this via <see cref="ItemAlias"/>.</summary>
public class GroceryItem
{
    public int Id { get; set; }

    public required string CanonicalName { get; set; }

    public string? Brand { get; set; }

    /// <summary>Pack size incl. unit, e.g. "500 g", "2 L", "6 pack".</summary>
    public string? Size { get; set; }

    public string? Barcode { get; set; }

    public string? Category { get; set; }

    public ICollection<ItemAlias> Aliases { get; set; } = [];
}
