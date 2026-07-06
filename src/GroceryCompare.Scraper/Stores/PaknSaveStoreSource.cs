using System.Text.Json;
using GroceryCompare.Domain.Scraping;
using Microsoft.Extensions.Logging;

namespace GroceryCompare.Scraper.Stores;

/// <summary>Fetches Pak'nSave store locations from its Next.js store-finder.
///
/// Two steps, both hitting pre-rendered pages (no auth needed):
///   1. GET /store-finder — the embedded __NEXT_DATA__ lists every store's URL.
///   2. GET each store URL — its __NEXT_DATA__ "page" node carries the name,
///      stable store_id, and contact details (address, lat/long, region).
///
/// This structure is inherently fragile (architecture-plan.md §7); if Foodstuffs
/// changes the site, only this adapter breaks. Pak'nSave and New World are both
/// Foodstuffs, so PBI-013 is expected to share most of this code.</summary>
public class PaknSaveStoreSource(HttpClient httpClient, ILogger<PaknSaveStoreSource> logger)
    : IStoreSource
{
    public string FranchiseName => "Pak'nSave";

    public async Task<IReadOnlyList<ScrapedStore>> GetStoresAsync(
        CancellationToken cancellationToken = default)
    {
        var finderHtml = await httpClient.GetStringAsync("/store-finder", cancellationToken);
        var storeUrls = ParseStoreUrls(finderHtml);
        logger.LogInformation("Pak'nSave store-finder listed {Count} stores", storeUrls.Count);

        var stores = new List<ScrapedStore>(storeUrls.Count);
        foreach (var url in storeUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var pageHtml = await httpClient.GetStringAsync(url, cancellationToken);
                if (ParseStorePage(pageHtml) is { } store)
                {
                    stores.Add(store);
                }
                else
                {
                    logger.LogWarning("Pak'nSave store page {Url} had no parseable detail", url);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or FormatException or JsonException)
            {
                // One unreachable or malformed store page shouldn't abort the
                // whole directory sync (architecture-plan.md §7, resilience).
                logger.LogWarning(ex, "Skipping Pak'nSave store page {Url}", url);
            }
        }

        return stores;
    }

    /// <summary>Extracts store page URLs from the store-finder's contentstackStores list.</summary>
    internal static List<string> ParseStoreUrls(string storeFinderHtml)
    {
        var root = NextData.Parse(storeFinderHtml);
        var stores = root.Get("props", "pageProps", "contentstackStores");
        if (stores is not { ValueKind: JsonValueKind.Array } array)
        {
            return [];
        }

        var urls = new List<string>();
        foreach (var store in array.EnumerateArray())
        {
            if (store.GetString("url") is { Length: > 0 } url)
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    /// <summary>Maps a single store page's __NEXT_DATA__ to a ScrapedStore, or null
    /// if the essential fields (code + name) are absent.</summary>
    internal static ScrapedStore? ParseStorePage(string storePageHtml)
    {
        var page = NextData.Parse(storePageHtml).Get("props", "pageProps", "page");
        if (page is not { } p)
        {
            return null;
        }

        var code = p.GetString("store_id");
        var name = p.GetString("title");
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
        {
            return null;
        }

        var contact = p.Get("contact_details");
        return new ScrapedStore
        {
            ExternalStoreCode = code,
            Name = name,
            Address = contact?.GetString("address"),
            // Pak'nSave has no discrete suburb field; PBI-016 handles cleanup.
            Suburb = null,
            Region = contact?.GetString("region"),
            Latitude = contact?.GetDouble("latitude"),
            Longitude = contact?.GetDouble("longitude"),
        };
    }
}
