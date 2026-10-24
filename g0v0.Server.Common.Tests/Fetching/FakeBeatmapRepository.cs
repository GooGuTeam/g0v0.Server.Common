// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;

namespace g0v0.Server.Common.Tests.Fetching;

internal sealed class FakeBeatmapRepository : IBeatmapRepository
{
    private readonly Dictionary<int, Beatmap> _beatmaps = new();

    public Task<Beatmap?> GetByIdAsync(int beatmapId)
    {
        return Task.FromResult(_beatmaps.TryGetValue(beatmapId, out Beatmap? beatmap) ? beatmap : null);
    }

    public Task<Beatmap?> GetByChecksumAsync(string checksum)
    {
        Beatmap? match = _beatmaps.Values.FirstOrDefault(beatmap => beatmap.Checksum == checksum);
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<Beatmap>> GetByFilenameAsync(string filename)
    {
        string? version = BeatmapFilename.ExtractVersion(filename);
        IReadOnlyList<Beatmap> matches = version is null
            ? []
            : _beatmaps.Values.Where(beatmap => beatmap.Version == version).ToArray();
        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<Beatmap>> GetByBeatmapSetIdAsync(int beatmapSetId)
    {
        IReadOnlyList<Beatmap> matches = _beatmaps.Values.Where(beatmap => beatmap.BeatmapSetId == beatmapSetId).ToArray();
        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<Beatmap>> GetByMapperIdAsync(int mapperId)
    {
        IReadOnlyList<Beatmap> matches = _beatmaps.Values.Where(beatmap => beatmap.MapperId == mapperId).ToArray();
        return Task.FromResult(matches);
    }

    public async Task<Beatmap> GetOrFetchByIdAsync(int beatmapId, IFetcher fetcher, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(beatmapId).ConfigureAwait(false)
               ?? await fetcher.FetchBeatmapAsync(beatmapId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Beatmap> GetOrFetchByChecksumAsync(string checksum, IFetcher fetcher, CancellationToken cancellationToken = default)
    {
        return await GetByChecksumAsync(checksum).ConfigureAwait(false)
               ?? await fetcher.FetchBeatmapAsync(checksum, cancellationToken).ConfigureAwait(false);
    }

    public Task CreateAsync(Beatmap beatmap)
    {
        _beatmaps[beatmap.Id] = beatmap;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Beatmap beatmap)
    {
        _beatmaps[beatmap.Id] = beatmap;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Beatmap beatmap)
    {
        _beatmaps.Remove(beatmap.Id);
        return Task.CompletedTask;
    }
}
