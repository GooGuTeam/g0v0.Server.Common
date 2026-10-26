// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using g0v0.Server.Common.Beatmaps;
using g0v0.Server.Common.Fetching;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Fetching;

public partial class FetcherTests
{
    [Test]
    public async Task SearchUsesAuthenticatedEncodedFiltersAndReturnsDetachedMetadata()
    {
        string response = "{\"beatmapsets\":[" + SetResponse().Content + "],\"total\":200,\"cursor\":{\"id\":55}}";
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-1", 3600)),
            ("/api/v2/beatmapsets/search", new StubResponse(HttpStatusCode.OK, response)));
        FakeBeatmapSetRepository repository = new();
        using Fetcher fetcher = CreateFetcher(handler, new FakeStringCache(), repository);

        BeatmapSearchQuery filters = new()
        {
            Query = "artist=a & b",
            Mode = 3,
            Status = "ranked",
            Sort = "rating_desc",
            Extras = "video",
            IncludeExplicitContent = true
        };
        BeatmapSearchResult result = await fetcher.FetchSearchAsync(filters, "abc+/=");

        Assert.That(result.BeatmapSets.Single().Id, Is.EqualTo(55));
        Assert.That(result.BeatmapSets.Single().Beatmaps, Has.Count.EqualTo(2));
        Assert.That(result.Total, Is.EqualTo(200));
        Assert.That(Encoding.UTF8.GetString(Convert.FromBase64String(result.Cursor!)), Is.EqualTo("{\"id\":55}"));
        Assert.That(repository.Sets, Is.Empty, "partial search metadata must not replace full persisted records");
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(new Uri(handler.RequestUris.Last()).Query);
        Assert.That(query["q"].ToString(), Is.EqualTo("artist=a & b"));
        Assert.That(query["m"].ToString(), Is.EqualTo("3"));
        Assert.That(query["s"].ToString(), Is.EqualTo("ranked"));
        Assert.That(query["sort"].ToString(), Is.EqualTo("rating_desc"));
        Assert.That(query["e"].ToString(), Is.EqualTo("video"));
        Assert.That(query["cursor_string"].ToString(), Is.EqualTo("abc+/="));
        Assert.That(query["nsfw"].ToString(), Is.EqualTo("true"));
    }

    [TestCase("{}")]
    [TestCase("{\"beatmapsets\":null,\"total\":1}")]
    public void SearchRejectsMalformedUpstreamPages(string payload)
    {
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-1", 3600)),
            ("/api/v2/beatmapsets/search", new StubResponse(HttpStatusCode.OK, payload)));
        using Fetcher fetcher = CreateFetcher(handler, new FakeStringCache(), new FakeBeatmapSetRepository());
        Task<BeatmapSearchResult> request = fetcher.FetchSearchAsync(new BeatmapSearchQuery(), cursor: null);
        Assert.ThrowsAsync<FetcherException>(async () => await request);
    }
}