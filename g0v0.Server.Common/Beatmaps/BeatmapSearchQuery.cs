// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Globalization;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Public osu! beatmapset search filters, independent of any client protocol.</summary>
public sealed record BeatmapSearchQuery
{
    /// <summary>Gets the keywords and osu! search expressions.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Gets the category, such as leaderboard, ranked, pending, or any.</summary>
    public string Status { get; init; } = "leaderboard";

    /// <summary>Gets the sort field and direction.</summary>
    public string Sort { get; init; } = "ranked_desc";

    /// <summary>Gets the legacy ruleset ID, or null for all rulesets.</summary>
    public int? Mode { get; init; }

    /// <summary>Gets the language filter.</summary>
    public string Language { get; init; } = "any";

    /// <summary>Gets dot-separated general filters, for example converts or featured_artists.</summary>
    public string General { get; init; } = string.Empty;

    /// <summary>Gets dot-separated extras, for example video or storyboard.</summary>
    public string Extras { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether explicit content is included.</summary>
    public bool IncludeExplicitContent { get; init; }

    internal BeatmapSearchQuery Normalize()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Mode ?? 0, 0, nameof(Mode));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Mode ?? 0, 3, nameof(Mode));
        ArgumentException.ThrowIfNullOrWhiteSpace(Status);
        ArgumentException.ThrowIfNullOrWhiteSpace(Sort);
        return this with
        {
            Query = Query.Trim(),
            Status = Status.Trim(),
            Sort = Sort.Trim(),
            Language = string.IsNullOrWhiteSpace(Language) ? "any" : Language.Trim(),
            General = NormalizeFilters(General),
            Extras = NormalizeFilters(Extras)
        };
    }

    internal SortedDictionary<string, string> GetParameters(string? cursor)
    {
        SortedDictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["q"] = Query,
            ["s"] = Status,
            ["sort"] = Sort,
            ["nsfw"] = IncludeExplicitContent ? "true" : "false"
        };
        if (Mode is { } mode)
        {
            parameters["m"] = mode.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.Equals(Language, "any", StringComparison.Ordinal))
        {
            parameters["l"] = Language;
        }

        if (General.Length > 0)
        {
            parameters["c"] = General;
        }

        if (Extras.Length > 0)
        {
            parameters["e"] = Extras;
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            parameters["cursor_string"] = cursor;
        }

        return parameters;
    }

    private static string NormalizeFilters(string filters) => string.Join(
        '.',
        filters.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
}