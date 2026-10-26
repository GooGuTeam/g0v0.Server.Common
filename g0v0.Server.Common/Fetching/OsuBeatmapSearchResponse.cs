// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace g0v0.Server.Common.Fetching;

internal sealed class OsuBeatmapSearchResponse
{
    [JsonProperty("beatmapsets")]
    public OsuBeatmapSet[]? BeatmapSets { get; set; }

    [JsonProperty("total")]
    public long? Total { get; set; }

    [JsonProperty("cursor_string")]
    public string? CursorString { get; set; }

    [JsonProperty("cursor")]
    public JObject? Cursor { get; set; }
}