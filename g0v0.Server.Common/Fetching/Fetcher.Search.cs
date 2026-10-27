// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Text;
using g0v0.Server.Common.Beatmaps;
using g0v0.Server.Common.Database.Models;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Fetching;

public partial class Fetcher
{
    /// <inheritdoc/>
    public async Task<BeatmapSearchResult> FetchSearchAsync(BeatmapSearchQuery query, string? cursor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string parameters = string.Join('&', query.Normalize().GetParameters(cursor)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        OsuBeatmapSearchResponse response = await GetJsonAsync<OsuBeatmapSearchResponse>(
            "https://osu.ppy.sh/api/v2/beatmapsets/search?" + parameters, cancellationToken).ConfigureAwait(false);
        if (response.BeatmapSets is null || response.Total is null or < 0)
        {
            throw new FetcherException("The osu! API returned an invalid beatmap search response.");
        }

        List<BeatmapSet> sets = [];
        foreach (OsuBeatmapSet dto in response.BeatmapSets)
        {
            BeatmapSet set = MapSet(dto);
            foreach (OsuBeatmap difficulty in (dto.Beatmaps ?? []).Concat(dto.Converts ?? []).DistinctBy(map => (map.Id, map.ModeInt)))
            {
                set.Beatmaps.Add(MapBeatmap(difficulty, set.Id));
            }

            sets.Add(set);
        }

        string? nextCursor = response.CursorString;
        if (string.IsNullOrEmpty(nextCursor) && response.Cursor is { Count: > 0 })
        {
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(response.Cursor.ToString(Formatting.None)));
        }

        return new BeatmapSearchResult
        {
            BeatmapSets = sets.ToArray(),
            Total = response.Total.Value,
            Cursor = string.IsNullOrEmpty(nextCursor) ? null : nextCursor
        };
    }
}