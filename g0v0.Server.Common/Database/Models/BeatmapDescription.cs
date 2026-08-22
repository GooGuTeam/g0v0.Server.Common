// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the description of a beatmap set, as returned by the osu! API.
/// </summary>
public class BeatmapDescription
{
    /// <summary>
    /// Gets or sets the BBCode description.
    /// </summary>
    public string? Bbcode { get; set; }

    /// <summary>
    /// Gets or sets the rendered HTML description.
    /// </summary>
    public string? Description { get; set; }
}