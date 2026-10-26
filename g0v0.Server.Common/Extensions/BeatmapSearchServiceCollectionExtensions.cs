// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Beatmaps;
using g0v0.Server.Common.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace g0v0.Server.Common.Extensions;

/// <summary>Registers the reusable cached search service and its optional warmup worker.</summary>
public static class BeatmapSearchServiceCollectionExtensions
{
    /// <summary>Adds cached search after AddFetcher, AddCache, and repository registration.</summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddBeatmapSearch(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<BackgroundTaskRunner>();
        services.TryAddSingleton<IBackgroundTaskRunner>(provider => provider.GetRequiredService<BackgroundTaskRunner>());
        services.TryAddSingleton<BeatmapSearchService>();
        services.TryAddSingleton<IBeatmapSearchService>(provider => provider.GetRequiredService<BeatmapSearchService>());
        services.AddHostedService<BeatmapSearchWarmupService>();
        return services;
    }
}