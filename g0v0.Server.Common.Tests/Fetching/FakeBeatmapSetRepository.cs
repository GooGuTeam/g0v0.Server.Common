// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;

namespace g0v0.Server.Common.Tests.Fetching;

/// <summary>
/// In-memory <see cref="IBeatmapSetRepository"/> used by Fetcher tests.
/// </summary>
internal sealed class FakeBeatmapSetRepository : IBeatmapSetRepository
{
    private readonly Dictionary<int, BeatmapSet> _sets = new();

    public IReadOnlyDictionary<int, BeatmapSet> Sets => _sets;

    public Task<BeatmapSet?> GetByIdAsync(int beatmapSetId)
    {
        return Task.FromResult(_sets.TryGetValue(beatmapSetId, out BeatmapSet? set) ? set : null);
    }

    public Task<BeatmapSet?> GetByIdWithBeatmapsAsync(int beatmapSetId)
    {
        return GetByIdAsync(beatmapSetId);
    }

    public Task<BeatmapSet> UpsertWithBeatmapsAsync(BeatmapSet beatmapSet)
    {
        _sets[beatmapSet.Id] = beatmapSet;
        return Task.FromResult(beatmapSet);
    }

    public Task DeleteAsync(BeatmapSet beatmapSet)
    {
        _sets.Remove(beatmapSet.Id);
        return Task.CompletedTask;
    }
}