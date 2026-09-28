// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Database.Configurations;

internal static class ConfigurationHelper
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

    private static readonly Dictionary<BeatmapOnlineStatus, string> StatusToDatabaseValue = new()
    {
        [BeatmapOnlineStatus.Graveyard] = "GRAVEYARD",
        [BeatmapOnlineStatus.WIP] = "WIP",
        [BeatmapOnlineStatus.Pending] = "PENDING",
        [BeatmapOnlineStatus.Ranked] = "RANKED",
        [BeatmapOnlineStatus.Approved] = "APPROVED",
        [BeatmapOnlineStatus.Qualified] = "QUALIFIED",
        [BeatmapOnlineStatus.Loved] = "LOVED",
    };

    private static readonly Dictionary<string, BeatmapOnlineStatus> DatabaseValueToStatus =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GRAVEYARD"] = BeatmapOnlineStatus.Graveyard,
            ["WIP"] = BeatmapOnlineStatus.WIP,
            ["PENDING"] = BeatmapOnlineStatus.Pending,
            ["RANKED"] = BeatmapOnlineStatus.Ranked,
            ["APPROVED"] = BeatmapOnlineStatus.Approved,
            ["QUALIFIED"] = BeatmapOnlineStatus.Qualified,
            ["LOVED"] = BeatmapOnlineStatus.Loved,
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

    public static string ConvertBeatmapStatusToDatabaseValue(BeatmapOnlineStatus value)
    {
        return StatusToDatabaseValue.TryGetValue(value, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported beatmap status '{value}'.");
    }

    public static BeatmapOnlineStatus ConvertDatabaseValueToBeatmapStatus(string value)
    {
        return DatabaseValueToStatus.TryGetValue(value, out BeatmapOnlineStatus status)
            ? status
            : throw new InvalidOperationException($"Unsupported beatmap status '{value}'.");
    }

    public static string ConvertRankToDatabaseValue(ScoreRank value)
    {
        return SupportedRanks.Contains(value)
            ? value.ToString().ToUpperInvariant()
            : throw new InvalidOperationException($"Unsupported score rank '{value}'.");
    }

    public static ScoreRank ConvertDatabaseValueToRank(string value)
    {
        return Enum.TryParse(value: value, ignoreCase: true, out ScoreRank rank) && SupportedRanks.Contains(rank)
            ? rank
            : throw new InvalidOperationException($"Unsupported score rank '{value}'.");
    }

    public static string? SerializeMods(IList<APIMod>? mods)
    {
        // Empty lists are serialized as "[]" (never NULL): the playlist mods
        // must always be a list rather than null.
        return JsonConvert.SerializeObject(mods ?? new List<APIMod>(), JsonSettings);
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

        Dictionary<string, int> payload = statistics
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

        Dictionary<string, int> payload = JsonConvert.DeserializeObject<Dictionary<string, int>>(value, JsonSettings) ??
                      new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<HitResult, int> statistics = new();

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

    public static DateTime? ConvertNullableDateTimeOffsetToDateTime(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.UtcDateTime : null;
    }

    public static DateTimeOffset? ConvertNullableDateTimeToDateTimeOffset(DateTime? value)
    {
        return value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }

    /// <summary>
    /// Converts an enum value to its legacy MySQL native-enum representation
    /// (PascalCase to UPPER_SNAKE_CASE, e.g. <c>HostOnly</c> to <c>HOST_ONLY</c>).
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum value.</param>
    /// <returns>The legacy database representation.</returns>
    public static string ConvertEnumToDatabaseValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();

        Span<char> buffer = stackalloc char[(name.Length * 2) + 1];
        int writeIndex = 0;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                buffer[writeIndex++] = '_';
            }

            buffer[writeIndex++] = char.ToUpperInvariant(c);
        }

        return new string(buffer[..writeIndex]);
    }

    /// <summary>
    /// Converts a legacy MySQL native-enum value back to its enum representation.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The legacy database value.</param>
    /// <returns>The parsed enum value.</returns>
    public static TEnum ConvertDatabaseValueToEnum<TEnum>(string value)
        where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
    }

    private static bool TryParseHitResult(string key, out HitResult result)
    {
        return Enum.TryParse(value: key, ignoreCase: true, out result) ? true : Enum.TryParse(value: key.ToPascalCase(), ignoreCase: true, out result);
    }
}