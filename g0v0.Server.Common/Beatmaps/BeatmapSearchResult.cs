// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>A detached page of beatmap sets and its upstream continuation cursor.</summary>
public sealed record BeatmapSearchResult
{
    /// <summary>Gets the matching sets, including their difficulty metadata.</summary>
    [JsonProperty(Required = Required.Always)]
    public BeatmapSet[] BeatmapSets { get; init; } = [];

    /// <summary>Gets the upstream total number of matches.</summary>
    [JsonProperty(Required = Required.Always)]
    public long Total { get; init; }

    /// <summary>Gets the cursor for the next page, or null at the end.</summary>
    public string? Cursor { get; init; }

    /// <summary>Gets a value indicating whether another upstream page exists.</summary>
    [JsonIgnore]
    public bool HasMore => !string.IsNullOrEmpty(Cursor);
}