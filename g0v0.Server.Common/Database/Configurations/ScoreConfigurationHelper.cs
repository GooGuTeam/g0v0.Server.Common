// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Database.Configurations;

internal static class ScoreConfigurationHelper
{
    private static readonly Dictionary<int, string> ModeToDatabaseValue = new()
    {
        [0] = "OSU",
        [1] = "TAIKO",
        [2] = "FRUITS",
        [3] = "MANIA",
        [10] = "SENTAKKI",
        [11] = "TAU",
        [12] = "RUSH",
        [13] = "HISHIGATA",
        [14] = "SOYOKAZE",
    };

    private static readonly Dictionary<string, int> DatabaseValueToMode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OSU"] = 0,
        ["TAIKO"] = 1,
        ["FRUITS"] = 2,
        ["MANIA"] = 3,
        ["OSURX"] = 0,
        ["OSUAP"] = 0,
        ["TAIKORX"] = 1,
        ["FRUITSRX"] = 2,
        ["SENTAKKI"] = 10,
        ["TAU"] = 11,
        ["RUSH"] = 12,
        ["HISHIGATA"] = 13,
        ["SOYOKAZE"] = 14,
    };

    private static readonly HashSet<ScoreRank> SupportedRanks =
    [
        ScoreRank.X,
        ScoreRank.XH,
        ScoreRank.S,
        ScoreRank.SH,
        ScoreRank.A,
        ScoreRank.B,
        ScoreRank.C,
        ScoreRank.D,
        ScoreRank.F,
    ];

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Converters =
        [
            new StringEnumConverter(),
        ],
    };

    public static ValueComparer<IList<APIMod>> ModsComparer { get; } = new(
        (left, right) => SerializeMods(left) == SerializeMods(right),
        value => (SerializeMods(value) ?? string.Empty).GetHashCode(StringComparison.Ordinal),
        value => DeserializeMods(SerializeMods(value)));

    public static ValueComparer<IDictionary<HitResult, int>> StatisticsComparer { get; } = new(
        (left, right) => SerializeStatistics(left) == SerializeStatistics(right),
        value => (SerializeStatistics(value) ?? string.Empty).GetHashCode(StringComparison.Ordinal),
        value => DeserializeStatistics(SerializeStatistics(value)));

    public static string ConvertModeToDatabaseValue(int value)
    {
        return ModeToDatabaseValue.TryGetValue(value, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported legacy score mode value '{value}'.");
    }

    public static int ConvertDatabaseValueToMode(string value)
    {
        return DatabaseValueToMode.TryGetValue(value, out int mode)
            ? mode
            : throw new InvalidOperationException($"Unsupported legacy score mode '{value}'.");
    }

    public static string ConvertRankToDatabaseValue(ScoreRank value)
    {
        return SupportedRanks.Contains(value)
            ? value.ToString().ToUpperInvariant()
            : throw new InvalidOperationException($"Unsupported score rank '{value}'.");
    }

    public static ScoreRank ConvertDatabaseValueToRank(string value)
    {
        return Enum.TryParse(value, true, out ScoreRank rank) && SupportedRanks.Contains(rank)
            ? rank
            : throw new InvalidOperationException($"Unsupported score rank '{value}'.");
    }

    public static string? SerializeMods(IList<APIMod>? mods)
    {
        return mods is { Count: > 0 }
            ? JsonConvert.SerializeObject(mods, JsonSettings)
            : null;
    }

    public static IList<APIMod> DeserializeMods(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? new List<APIMod>()
            : JsonConvert.DeserializeObject<List<APIMod>>(value, JsonSettings) ?? new List<APIMod>();
    }

    public static string? SerializeStatistics(IDictionary<HitResult, int>? statistics)
    {
        if (statistics == null || statistics.Count == 0)
        {
            return null;
        }

        var payload = statistics
            .OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key.ToString(), entry => entry.Value, StringComparer.Ordinal);

        return JsonConvert.SerializeObject(payload, JsonSettings);
    }

    public static IDictionary<HitResult, int> DeserializeStatistics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<HitResult, int>();
        }

        var payload = JsonConvert.DeserializeObject<Dictionary<string, int>>(value, JsonSettings) ?? new Dictionary<string, int>(StringComparer.Ordinal);
        var statistics = new Dictionary<HitResult, int>();

        foreach ((string key, int count) in payload)
        {
            if (!TryParseHitResult(key, out HitResult result))
            {
                throw new InvalidOperationException($"Unsupported hit result '{key}' in score statistics payload.");
            }

            statistics[result] = count;
        }

        return statistics;
    }

    private static bool TryParseHitResult(string key, out HitResult result)
    {
        if (Enum.TryParse(key, true, out result))
        {
            return true;
        }

        return Enum.TryParse(key.ToPascalCase(), true, out result);
    }
}