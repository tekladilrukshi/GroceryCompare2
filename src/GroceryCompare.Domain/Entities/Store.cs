namespace GroceryCompare.Domain.Entities;

/// <summary>A physical store location, populated by the store-directory sync.</summary>
public class Store
{
    public int Id { get; set; }

    public int FranchiseId { get; set; }

    public Franchise? Franchise { get; set; }

    public required string Name { get; set; }

    public string? Address { get; set; }

    public string? Suburb { get; set; }

    public string? Region { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    /// <summary>The retailer's own store identifier, used by the scraper.</summary>
    public required string ExternalStoreCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastSyncedAt { get; set; }
}
