// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Extracts the difficulty version from a stable <c>.osu</c> filename.
/// </summary>
/// <remarks>
/// The stable client identifies a local beatmap by its filename, which the game builds as
/// <c>Artist - Title (Mapper) [Version].osu</c>. Only the bracketed part is needed to find the
/// stored row, because <see cref="Models.Beatmap.Version"/> is stored without the rest of the name.
/// </remarks>
public static class BeatmapFilename
{
    /// <summary>
    /// Extracts the difficulty version from a filename.
    /// </summary>
    /// <param name="filename">The filename as sent by the client, with or without the <c>.osu</c> suffix.</param>
    /// <returns>The bracketed version, the whole name without its suffix when unbracketed, or <see langword="null"/> when the name is blank.</returns>
    public static string? ExtractVersion(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        string name = filename.Trim();
        if (name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        int open = name.LastIndexOf('[');
        int close = name.LastIndexOf(']');
        return open >= 0 && close > open
            ? name[(open + 1)..close]
            : name;
    }
}