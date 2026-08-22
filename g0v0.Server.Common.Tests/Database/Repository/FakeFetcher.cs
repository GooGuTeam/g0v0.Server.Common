// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Fetching;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Scriptable <see cref="IFetcher"/> used by repository GetOrFetch tests.
/// </summary>
internal sealed class FakeFetcher : IFetcher
{
    private readonly Func<int, Beatmap>? _beatmapFactory;
    private readonly Func<int, BeatmapSet>? _setFactory;
    private readonly Exception? _exception;

    public FakeFetcher(Func<int, Beatmap>? beatmapFactory = null, Func<int, BeatmapSet>? setFactory = null, Exception? exception = null)
    {
        _beatmapFactory = beatmapFactory;
        _setFactory = setFactory;
        _exception = exception;
    }

    public int BeatmapFetchCount { get; private set; }

    public int SetFetchCount { get; private set; }

    public Task<Beatmap> FetchBeatmapAsync(int beatmapId, CancellationToken cancellationToken = default)
    {
        BeatmapFetchCount++;
        return _exception != null
            ? throw _exception
            : Task.FromResult(_beatmapFactory?.Invoke(beatmapId) ?? new Beatmap { Id = beatmapId, BeatmapSetId = beatmapId, Version = "fetched", Mode = 0, DifficultyRating = 1.0, MapperId = 1, TotalLength = 1 });
    }

    public Task<Beatmap> FetchBeatmapAsync(string checksum, CancellationToken cancellationToken = default)
    {
        BeatmapFetchCount++;
        return _exception != null
            ? throw _exception
            : Task.FromResult(_beatmapFactory?.Invoke(0) ?? new Beatmap { Id = 0, BeatmapSetId = 0, Checksum = checksum, Version = "fetched", Mode = 0, DifficultyRating = 1.0, MapperId = 1, TotalLength = 1 });
    }

    public Task<BeatmapSet> FetchBeatmapSetAsync(int beatmapSetId, CancellationToken cancellationToken = default)
    {
        SetFetchCount++;
        return _exception != null
            ? throw _exception
            : Task.FromResult(_setFactory?.Invoke(beatmapSetId) ?? new BeatmapSet { Id = beatmapSetId, Artist = "fetched", Title = "Fetched Set", Creator = "mapper", CreatorId = 1, SubmittedDate = DateTimeOffset.UtcNow });
    }

    public Task<string> FetchBeatmapRawAsync(int beatmapId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}