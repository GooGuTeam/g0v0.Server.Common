// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Http;
using g0v0.Server.Common.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Shares Redis search pages and bounded background prefetching across client requests.</summary>
/// <param name="scopes">Creates independent scopes for upstream and database work.</param>
/// <param name="cache">The shared Redis string cache.</param>
/// <param name="background">Tracks speculative tasks through host shutdown.</param>
/// <param name="configs">The optional search configuration.</param>
/// <param name="clock">The clock used for background delays.</param>
/// <param name="logger">Search diagnostics.</param>
/// <param name="lifetime">Cancels shared upstream requests when the host stops.</param>
public sealed class BeatmapSearchService(
    IServiceScopeFactory scopes,
    IStringCache cache,
    IBackgroundTaskRunner background,
    ConfigManager configs,
    TimeProvider clock,
    ILogger<BeatmapSearchService> logger,
    IHostApplicationLifetime? lifetime = null) : IBeatmapSearchService
{
    private static readonly string[] HomepageSorts = ["ranked_desc", "updated_desc", "favourites_desc", "plays_desc"];
    private readonly BeatmapSearchConfig _options = (configs.TryGet<BeatmapSearchConfig>() ?? new BeatmapSearchConfig()).Validate();
    private readonly ConcurrentDictionary<string, Lazy<Task<BeatmapSearchResult>>> _requests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _prefetches = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<BeatmapSearchResult> SearchAsync(BeatmapSearchQuery query, string? cursor = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        query = query.Normalize();

        // Like lazer, client-credentials searches cannot apply account-specific upstream filters.
        return query.Status is "mine" or "favourites" || query.General.Split('.').Any(filter => filter is "recommended" or "follows")
            ? Task.FromResult(new BeatmapSearchResult())
            : GetPageAsync(query, cursor, _options.CacheLifetime, prefetch: true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BeatmapSet?> LookupAsync(int? beatmapSetId = null, int? beatmapId = null, string? checksum = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IServiceScope scope = scopes.CreateScope();
        IBeatmapSetRepository sets = scope.ServiceProvider.GetRequiredService<IBeatmapSetRepository>();
        IBeatmapRepository beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapRepository>();
        IFetcher fetcher = scope.ServiceProvider.GetRequiredService<IFetcher>();
        try
        {
            if (beatmapSetId is null)
            {
                Beatmap? beatmap;
                if (beatmapId is { } id)
                {
                    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id, nameof(beatmapId));
                    beatmap = await beatmaps.GetOrFetchByIdAsync(id, fetcher, cancellationToken).ConfigureAwait(false);
                }
                else if (!string.IsNullOrWhiteSpace(checksum))
                {
                    beatmap = await beatmaps.GetOrFetchByChecksumAsync(checksum, fetcher, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return null;
                }

                beatmapSetId = beatmap.BeatmapSetId;
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beatmapSetId.Value, nameof(beatmapSetId));
            return await sets.GetByIdWithBeatmapsAsync(beatmapSetId.Value).ConfigureAwait(false)
                ?? await fetcher.FetchBeatmapSetAsync(beatmapSetId.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpServiceException exception) when (exception.StatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>Warms lazer's four common homepages and up to two following pages for each.</summary>
    /// <param name="cancellationToken">Stops warmup during host shutdown.</param>
    /// <returns>The warmup operation.</returns>
    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        foreach (string sort in HomepageSorts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                BeatmapSearchQuery query = new() { Sort = sort };
                BeatmapSearchResult result = await GetPageAsync(query, cursor: null, _options.WarmupLifetime, prefetch: false, cancellationToken).ConfigureAwait(false);
                if (_options.EnablePrefetch)
                {
                    await PrefetchAsync(query, result.Cursor, pages: 2, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Could not warm beatmap searches sorted by {Sort}.", sort);
            }

            if (!string.Equals(sort, HomepageSorts[^1], StringComparison.Ordinal))
            {
                await Task.Delay(_options.WarmupQueryDelay, clock, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string CacheKey(BeatmapSearchQuery query, string? cursor)
    {
        string canonical = JsonConvert.SerializeObject(query.GetParameters(cursor));
        return "beatmapset:search:v2:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private async Task<BeatmapSearchResult?> ReadCacheAsync(string key)
    {
        try
        {
            string? value = await cache.GetAsync(key).ConfigureAwait(false);
            if (value is null)
            {
                return null;
            }

            BeatmapSearchResult? result = JsonConvert.DeserializeObject<BeatmapSearchResult>(value);
            return result is { BeatmapSets: not null, Total: >= 0 } ? result : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read beatmap search cache {CacheKey}; requesting the upstream page.", key);
            return null;
        }
    }

    private async Task<BeatmapSearchResult> GetPageAsync(
        BeatmapSearchQuery query, string? cursor, TimeSpan expiry, bool prefetch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = CacheKey(query, cursor);
        BeatmapSearchResult? cached = await ReadCacheAsync(key).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        Lazy<Task<BeatmapSearchResult>> request = _requests.GetOrAdd(key, _ => new Lazy<Task<BeatmapSearchResult>>(
            () => FetchPageAsync(key, query, cursor, expiry, prefetch)));

        // The caller may disconnect while another caller still needs the same upstream request.
        return await request.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<BeatmapSearchResult> FetchPageAsync(string key, BeatmapSearchQuery query, string? cursor, TimeSpan expiry, bool prefetch)
    {
        try
        {
            // A cache fill may have completed between the first read and joining this operation.
            BeatmapSearchResult? cached = await ReadCacheAsync(key).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            using IServiceScope scope = scopes.CreateScope();
            IBeatmapSearchSource source = scope.ServiceProvider.GetRequiredService<IBeatmapSearchSource>();
            BeatmapSearchResult result = await source.FetchSearchAsync(query, cursor, lifetime?.ApplicationStopping ?? CancellationToken.None).ConfigureAwait(false);
            try
            {
                await cache.SetAsync(key, JsonConvert.SerializeObject(result), expiry).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not cache beatmap search page {CacheKey}.", key);
            }

            if (prefetch && _options.EnablePrefetch && result.HasMore
                && (query.Query.Length > 0 || !string.Equals(query.Status, "leaderboard", StringComparison.Ordinal) || !string.IsNullOrEmpty(cursor)))
            {
                SchedulePrefetch(query, result.Cursor!);
            }

            return result;
        }
        finally
        {
            _requests.TryRemove(key, out _);
        }
    }

    private void SchedulePrefetch(BeatmapSearchQuery query, string cursor)
    {
        string key = CacheKey(query, cursor);
        if (_prefetches.Count >= 32 || !_prefetches.TryAdd(key, 0))
        {
            return;
        }

        try
        {
            _ = background.RunAsync(async cancellationToken =>
            {
                try
                {
                    await Task.Delay(_options.PrefetchDelay, clock, cancellationToken).ConfigureAwait(false);
                    await PrefetchAsync(query, cursor, pages: 1, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _prefetches.TryRemove(key, out _);
                }
            });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _prefetches.TryRemove(key, out _);
            logger.LogWarning(exception, "Could not schedule beatmap search prefetch.");
        }
    }

    private async Task PrefetchAsync(BeatmapSearchQuery query, string? cursor, int pages, CancellationToken cancellationToken)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int page = 0; page < pages && !string.IsNullOrEmpty(cursor) && seen.Add(cursor); page++)
        {
            if (page > 0)
            {
                await Task.Delay(_options.PrefetchPageDelay, clock, cancellationToken).ConfigureAwait(false);
            }

            BeatmapSearchResult result = await GetPageAsync(query, cursor, _options.PrefetchLifetime, prefetch: false, cancellationToken).ConfigureAwait(false);
            cursor = result.Cursor;
        }
    }
}