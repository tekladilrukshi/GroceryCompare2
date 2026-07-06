namespace GroceryCompare.Domain.Scraping;

/// <summary>A store location as returned by a retailer's store locator, before
/// it is reconciled into the <see cref="Entities.Store"/> table by the sync
/// (PBI-015). Franchise is implied by which <see cref="IStoreSource"/> produced it.</summary>
public sealed record ScrapedStore
{
    public required string ExternalStoreCode { get; init; }

    public required string Name { get; init; }

    public string? Address { get; init; }

    public string? Suburb { get; init; }

    public string? Region { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
