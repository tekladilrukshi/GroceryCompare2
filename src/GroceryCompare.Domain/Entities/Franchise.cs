namespace GroceryCompare.Domain.Entities;

/// <summary>One of the three supermarket chains: Pak'nSave, Woolworths NZ, New World.</summary>
public class Franchise
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ICollection<Store> Stores { get; set; } = [];
}
