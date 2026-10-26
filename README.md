# g0v0.Server.Common

This project is a part of g0v0 server v2.
It is the general library for g0v0 server series.

## Current Status

This project is under development. The content will be updated along with the v2 servers.

## Beatmap search

Register `AddBeatmapSearch()` after `AddFetcher()`, `AddCache()`, and the database repositories.
`IBeatmapSearchService.SearchAsync` accepts client-independent filters and an opaque upstream cursor;
`LookupAsync` resolves a set by set ID, beatmap ID, or checksum using the database and the existing fetcher.
Search pages contain detached metadata and do not overwrite complete database records.

Search requests use the fetcher's osu! OAuth credentials. Redis keys include all normalized filters and
the cursor. Concurrent misses for the same page share one upstream request per process; cancelling one
caller does not cancel other callers. A broken cache falls back to the upstream service. Account-specific
upstream categories (`mine`, `favourites`, `recommended`, and `follows`) return no results because the
fetcher uses client credentials.

The defaults follow lazer: requested pages live for 15 minutes; explicit searches, non-default categories,
and subsequent pages schedule one next page after 3 seconds, cached for 10 minutes. Background prefetch
scheduling is bounded and does not recursively traverse the whole result set. The hosted worker warms
four `leaderboard` homepages (`ranked_desc`, `updated_desc`, `favourites_desc`, and `plays_desc`) and up to
two following pages, starting 10 seconds after startup and repeating every 30 minutes. Homepages use a
20-minute lifetime. Host shutdown cancels background work.

Optional startup configuration in the server's `config/beatmap_search.json`:

```json
{
  "CacheLifetime": "00:15:00",
  "PrefetchLifetime": "00:10:00",
  "WarmupLifetime": "00:20:00",
  "EnablePrefetch": true,
  "PrefetchDelay": "00:00:03",
  "PrefetchPageDelay": "00:00:01.500",
  "EnableWarmup": true,
  "WarmupStartupDelay": "00:00:10",
  "WarmupInterval": "00:30:00",
  "WarmupQueryDelay": "00:00:02"
}
```

Lifetimes and the warmup interval must be positive; delays may be zero. These settings require a restart.

## License

g0v0 server v2 series is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please
see [the licence file](LICENCE) for more information.
[tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you **include the original
copyright and license notice in any copy of the software/source**.

"osu!" or "ppy" is a registered trademark of ppy Pty Ltd and is not affiliated with g0v0 server.