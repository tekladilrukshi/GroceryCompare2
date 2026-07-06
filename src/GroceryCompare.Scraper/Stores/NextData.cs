using System.Text.Json;
using System.Text.RegularExpressions;

namespace GroceryCompare.Scraper.Stores;

/// <summary>Helpers for pulling the embedded Next.js <c>__NEXT_DATA__</c> JSON
/// blob out of a server-rendered page. Foodstuffs sites (Pak'nSave, New World)
/// render store data into this blob, so no authenticated API call is needed for
/// the store directory — only price data (Phase 2) needs the guest JWT.</summary>
internal static partial class NextData
{
    [GeneratedRegex(
        """<script id="__NEXT_DATA__" type="application/json">(.*?)</script>""",
        RegexOptions.Singleline)]
    private static partial Regex NextDataScript();

    public static JsonElement Parse(string html)
    {
        var match = NextDataScript().Match(html);
        if (!match.Success)
        {
            throw new FormatException("No __NEXT_DATA__ script block found in page.");
        }

        using var doc = JsonDocument.Parse(match.Groups[1].Value);
        return doc.RootElement.Clone();
    }

    /// <summary>Navigates a chain of property names, returning null if any hop is
    /// missing rather than throwing — retailer pages vary between store types.</summary>
    public static JsonElement? Get(this JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(key, out var next))
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    public static string? GetString(this JsonElement element, params string[] path) =>
        element.Get(path) is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;

    public static double? GetDouble(this JsonElement element, params string[] path) =>
        element.Get(path) is { ValueKind: JsonValueKind.Number } n ? n.GetDouble() : null;
}
