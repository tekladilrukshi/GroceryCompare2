using System.Net;

namespace GroceryCompare.Scraper.Tests.Stores;

/// <summary>Serves recorded responses keyed by request path, so the adapter can
/// be driven end-to-end without any live network call.</summary>
internal sealed class StubHttpMessageHandler(IReadOnlyDictionary<string, string> responsesByPath)
    : HttpMessageHandler
{
    public List<string> RequestedPaths { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        RequestedPaths.Add(path);

        if (responsesByPath.TryGetValue(path, out var body))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
