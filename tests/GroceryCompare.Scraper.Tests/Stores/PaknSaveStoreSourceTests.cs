using GroceryCompare.Scraper.Stores;
using Microsoft.Extensions.Logging.Abstractions;

namespace GroceryCompare.Scraper.Tests.Stores;

public class PaknSaveStoreSourceTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void ParseStoreUrls_ReturnsEveryStoreUrl()
    {
        var urls = PaknSaveStoreSource.ParseStoreUrls(Fixture("store-finder.html"));

        Assert.Equal(
            ["/upper-north-island/auckland/albany", "/upper-north-island/auckland/henderson"],
            urls);
    }

    [Fact]
    public void ParseStorePage_MapsAllFields()
    {
        var store = PaknSaveStoreSource.ParseStorePage(Fixture("store-albany.html"));

        Assert.NotNull(store);
        Assert.Equal("65defcf2-bc15-490e-a84f-1f13b769cd22", store.ExternalStoreCode);
        Assert.Equal("PAK'nSAVE Albany", store.Name);
        Assert.Equal("Don McKinnon Drive, Albany, Auckland, 0632", store.Address);
        Assert.Equal("Upper North Island", store.Region);
        Assert.Equal(-36.729808878994575, store.Latitude);
        Assert.Equal(174.70680696588443, store.Longitude);
    }

    [Fact]
    public void ParseStorePage_MissingNextData_Throws()
    {
        // A page with no __NEXT_DATA__ is malformed; GetStoresAsync catches this
        // per-store so one bad page doesn't abort the sync (see resilience test).
        Assert.Throws<FormatException>(() =>
            PaknSaveStoreSource.ParseStorePage("<html><body>no next data here</body></html>"));
    }

    [Fact]
    public async Task GetStoresAsync_FetchesFinderThenEachStore()
    {
        var handler = new StubHttpMessageHandler(new Dictionary<string, string>
        {
            ["/store-finder"] = Fixture("store-finder.html"),
            ["/upper-north-island/auckland/albany"] = Fixture("store-albany.html"),
            ["/upper-north-island/auckland/henderson"] = Fixture("store-henderson.html"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.paknsave.co.nz") };
        var source = new PaknSaveStoreSource(httpClient, NullLogger<PaknSaveStoreSource>.Instance);

        var stores = await source.GetStoresAsync();

        Assert.Equal("Pak'nSave", source.FranchiseName);
        Assert.Equal(2, stores.Count);
        Assert.Contains(stores, s => s.Name == "PAK'nSAVE Albany");
        Assert.Contains(stores, s => s.Name == "PAK'nSAVE Henderson");
        Assert.Equal(
            ["/store-finder", "/upper-north-island/auckland/albany", "/upper-north-island/auckland/henderson"],
            handler.RequestedPaths);
    }

    [Fact]
    public async Task GetStoresAsync_SkipsStoreThatFailsToFetch()
    {
        // Henderson missing from the stub -> 404 -> logged and skipped, not fatal.
        var handler = new StubHttpMessageHandler(new Dictionary<string, string>
        {
            ["/store-finder"] = Fixture("store-finder.html"),
            ["/upper-north-island/auckland/albany"] = Fixture("store-albany.html"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.paknsave.co.nz") };
        var source = new PaknSaveStoreSource(httpClient, NullLogger<PaknSaveStoreSource>.Instance);

        var stores = await source.GetStoresAsync();

        var store = Assert.Single(stores);
        Assert.Equal("PAK'nSAVE Albany", store.Name);
    }
}
