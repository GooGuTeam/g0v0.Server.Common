// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Periodically warms common search pages without delaying host startup.</summary>
/// <param name="search">The shared search service.</param>
/// <param name="configs">The optional warmup settings.</param>
/// <param name="clock">The clock used for scheduling.</param>
/// <param name="logger">Warmup diagnostics.</param>
public sealed class BeatmapSearchWarmupService(
    BeatmapSearchService search,
    ConfigManager configs,
    TimeProvider clock,
    ILogger<BeatmapSearchWarmupService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BeatmapSearchConfig options = configs.TryGet<BeatmapSearchConfig>() ?? new BeatmapSearchConfig();
        if (!options.EnableWarmup)
        {
            return;
        }

        await Task.Delay(options.WarmupStartupDelay, clock, stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await search.WarmupAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Beatmap search warmup failed.");
            }

            await Task.Delay(options.WarmupInterval, clock, stoppingToken).ConfigureAwait(false);
        }
    }
}