using GroceryCompare.Domain.Entities;

namespace GroceryCompare.Domain.Scraping;

/// <summary>One implementation per franchise, isolating retailer-site breakage
/// to a single adapter (architecture-plan.md §7).</summary>
public interface IStoreSource
{
    /// <summary>Which franchise this adapter fetches stores for.</summary>
    string FranchiseName { get; }

    /// <summary>Fetches the current list of store locations from the retailer.</summary>
    Task<IReadOnlyList<ScrapedStore>> GetStoresAsync(CancellationToken cancellationToken = default);
}
