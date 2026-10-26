// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>Controls the shared search cache, speculative next-page requests, and homepage warming.</summary>
[ConfigFile("beatmap_search")]
public class BeatmapSearchConfig
{
    /// <summary>Gets or sets the lifetime of a requested search page.</summary>
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Gets or sets the lifetime of a prefetched page.</summary>
    public TimeSpan PrefetchLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Gets or sets the lifetime of a warmed homepage.</summary>
    public TimeSpan WarmupLifetime { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>Gets or sets a value indicating whether searches may prefetch their next page.</summary>
    public bool EnablePrefetch { get; set; } = true;

    /// <summary>Gets or sets the delay before speculative next-page loading.</summary>
    public TimeSpan PrefetchDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Gets or sets the delay between additional prefetched pages.</summary>
    public TimeSpan PrefetchPageDelay { get; set; } = TimeSpan.FromSeconds(1.5);

    /// <summary>Gets or sets a value indicating whether the host periodically warms common searches.</summary>
    public bool EnableWarmup { get; set; } = true;

    /// <summary>Gets or sets the delay before the first homepage warmup.</summary>
    public TimeSpan WarmupStartupDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets the interval between homepage warmups.</summary>
    public TimeSpan WarmupInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the delay between homepage queries.</summary>
    public TimeSpan WarmupQueryDelay { get; set; } = TimeSpan.FromSeconds(2);

    internal BeatmapSearchConfig Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(CacheLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PrefetchLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(WarmupLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(WarmupInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(PrefetchDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(PrefetchPageDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(WarmupStartupDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(WarmupQueryDelay, TimeSpan.Zero);
        return this;
    }
}