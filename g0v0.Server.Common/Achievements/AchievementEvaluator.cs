// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Rulesets;
using g0v0.Server.Common.Utils;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;
using BeatmapSet = g0v0.Server.Common.Database.Models.BeatmapSet;
using Score = g0v0.Server.Common.Database.Models.Score;
using UserStatistics = g0v0.Server.Common.Database.Models.UserStatistics;

namespace g0v0.Server.Common.Achievements;

/// <summary>Evaluates medals using processed scores and mod-adjusted difficulty.</summary>
/// <param name="beatmaps">Beatmap metadata.</param>
/// <param name="sets">Set metadata.</param>
/// <param name="statistics">Post-processing user statistics.</param>
/// <param name="users">Account eligibility.</param>
/// <param name="fetcher">Original beatmap files.</param>
/// <param name="rulesets">The installed rulesets.</param>
/// <param name="attributes">The difficulty calculator.</param>
public sealed class AchievementEvaluator(
    IBeatmapRepository beatmaps,
    IBeatmapSetRepository sets,
    IUserStatisticsRepository statistics,
    IUserRepository users,
    IFetcher fetcher,
    RulesetManager rulesets,
    ScoreAttributeUtils attributes) : IAchievementEvaluator
{
    private static readonly MedalRule[] Rules = LoadRules();

    /// <summary>Gets the built-in score-based medal catalogue.</summary>
    public static IReadOnlyList<Achievement> Definitions { get; } = Array.AsReadOnly(Rules.Select(rule => rule.Definition).ToArray());

    /// <summary>Evaluates already loaded inputs without performing I/O.</summary>
    /// <param name="context">The authoritative gameplay facts.</param>
    /// <param name="owned">The already unlocked IDs.</param>
    /// <returns>The eligible definitions, ordered by ID.</returns>
    public static IReadOnlyList<Achievement> Evaluate(AchievementEvaluationContext context, IReadOnlySet<int> owned)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(owned);
        return !context.Score.Processed || context.Score.Mode is < 0 or > 3
                                     || context.Mods.Any(mod => mod.Acronym is "AT" or "CN" or "RX" or "AP")
            ? []
            : (IReadOnlyList<Achievement>)Rules.Where(rule => !owned.Contains(rule.Definition.Id) && Matches(rule, context)).Select(rule => rule.Definition).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Achievement>> EvaluateAsync(Score score, IReadOnlySet<int> owned, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        if (!score.Processed || score.Mode is < 0 or > 3 || (await users.GetByIdAsync(score.UserId).ConfigureAwait(false)) is not { IsActive: true })
        {
            return [];
        }

        Ruleset ruleset = rulesets.GetRuleset(score.Mode);
        Mod[] mods = score.Mods.Select(mod => mod.ToMod(ruleset)).Where(mod => mod is not ModClassic).ToArray();
        if (mods.Any(mod => mod.Acronym is "AT" or "CN" or "RX" or "AP"))
        {
            return [];
        }

        Beatmap? beatmap = await beatmaps.GetByIdAsync(score.BeatmapId).ConfigureAwait(false);
        if (beatmap is null)
        {
            return [];
        }

        DifficultyAttributes difficulty = new();
        if (score.Passed && Rules.Any(rule => !owned.Contains(rule.Definition.Id) && NeedsDifficulty(rule.Rule)))
        {
            string content = await fetcher.FetchBeatmapRawAsync(score.BeatmapId, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA5351 // The score identifies the original beatmap using the osu! protocol's MD5.
            if (!string.Equals(Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(content))), score.BeatmapChecksum, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("\uFEFF" + content))), score.BeatmapChecksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The achievement beatmap does not match the processed score.");
            }

#pragma warning restore CA5351
            difficulty = attributes.CalculateDifficultyAttributes(ScoreAttributeUtils.DecodeBeatmap(content), score.Mode, score.Mods, cancellationToken);
        }

        BeatmapSet? set = await sets.GetByIdAsync(beatmap.BeatmapSetId).ConfigureAwait(false);
        UserStatistics? stats = await statistics.GetByUserIdAndModeAsync(score.UserId, score.Mode).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return Evaluate(new AchievementEvaluationContext(score, beatmap, set, stats, difficulty, mods), owned);
    }

    private static MedalRule[] LoadRules()
    {
        using Stream stream = typeof(AchievementEvaluator).Assembly.GetManifestResourceStream("g0v0.Server.Common.Achievements.medals.json")
                              ?? throw new InvalidOperationException("The achievement catalogue is missing.");
        using StreamReader reader = new(stream);
        return JsonConvert.DeserializeObject<MedalRule[]>(reader.ReadToEnd())
               ?? throw new InvalidDataException("The achievement catalogue is invalid.");
    }

    private static bool NeedsDifficulty(string rule) => rule is
        "skill_pass" or "skill_fc" or "nonstop" or "prepared" or "reckless_adandon" or "camera_shy" or
        "the_sun_of_all_fears" or "hour_before_the_down" or "slow_and_steady" or "no_time_to_spare" or
        "realtor_extraordinaire" or "impeccable" or "aeon" or "deliberation";

    private static bool Matches(MedalRule rule, AchievementEvaluationContext context)
    {
        Score score = context.Score;
        Beatmap beatmap = context.Beatmap;
        BeatmapSet? set = context.Set;
        if (rule.Mode >= 0 && rule.Mode != score.Mode)
        {
            return false;
        }

        bool ranked = beatmap.Status is BeatmapOnlineStatus.Ranked or BeatmapOnlineStatus.Approved;
        bool leaderboard = ranked || beatmap.Status is BeatmapOnlineStatus.Qualified or BeatmapOnlineStatus.Loved;
        bool pass = score.Passed;
        double stars = context.Difficulty.StarRating;
        double rate = osu.Game.Utils.ModUtils.CalculateRateWithMods(context.Mods);
        int misses = score.Statistics.TryGetValue(HitResult.Miss, out int missCount) ? missCount : 0;

        // PFC requires the theoretical maximum combo without misses; it does not require SS accuracy.
        bool fullCombo = pass && context.Difficulty.MaxCombo > 0 && score.MaxCombo == context.Difficulty.MaxCombo && misses == 0;
        HashSet<string> mods = context.Mods.Select(mod => mod.Acronym).ToHashSet(StringComparer.Ordinal);
        bool skill = pass && ranked && context.Mods.All(mod => mod.Ranked && mod.Type != ModType.DifficultyReduction) && Range(stars);
        return rule.Rule switch
        {
            "skill_pass" => skill,
            "skill_fc" => skill && fullCombo,
            "combo" => pass && ranked && Range(score.MaxCombo),
            "playcount" => context.Statistics is { } stats && Range(stats.PlayCount),
            "tth" => context.Statistics is { } stats && Range(stats.TotalHits),
            "mod" => pass && leaderboard && mods.Count == 1 && Has(rule.Mod!),
            "category_mod" => pass && leaderboard && context.Mods.Count > 0 && context.Mods.All(mod => string.Equals(mod.Type.ToString(), rule.Mod, StringComparison.Ordinal)),

            // Jackpot: Pass with a score above 100,000 whose digits are all identical; Stable uses the classic score (https://inex.osekai.net/medals/Jackpot/)
            "jackpot" => pass && RepeatedDigits(score.ClassicTotalScore > 0 ? score.ClassicTotalScore : score.TotalScore),

            // Nonstop: PFC a beatmap with a drain time longer than 8:41 before mods (https://inex.osekai.net/medals/Nonstop/)
            "nonstop" => fullCombo && beatmap.HitLength > 521,

            // Time Dilation: Pass with DT or NC and a drain time longer than 8 minutes after mods, without NF (https://inex.osekai.net/medals/Time%20Dilation/)
            "time_dilation" => pass && Fast() && !Has("NF") && beatmap.HitLength / rate > 480,

            // To The Core: Pass with DT or NC when the title or artist name contains Nightcore (https://inex.osekai.net/medals/To%20The%20Core/)
            "to_the_core" => pass && Fast() && set is not null && (set.Title.Contains("Nightcore", StringComparison.Ordinal) || set.Artist.Contains("Nightcore", StringComparison.Ordinal)),

            // When You See It: Pass a beatmap whose artist name contains lowercase xi with a displayed accuracy of X7.27% (https://inex.osekai.net/medals/When%20You%20See%20It/)
            "wysi" => pass && set is not null && set.Artist.Contains("xi", StringComparison.Ordinal) && (int)Math.Round(score.Accuracy * 10000) % 1000 == 727,

            // Prepared: PFC any beatmap with NF (https://inex.osekai.net/medals/Prepared/)
            "prepared" => fullCombo && Has("NF"),

            // Reckless Abandon: PFC with HR and SD on a beatmap rated at least 3 stars after mods (https://inex.osekai.net/medals/Reckless%20Abandon/)
            "reckless_adandon" => fullCombo && Has("HR") && Has("SD") && stars >= 3,

            // Lights Out: Pass any beatmap with FL and NC (https://inex.osekai.net/medals/Lights%20Out/)
            "lights_out" => pass && Has("FL") && Has("NC"),

            // Camera Shy: PFC any beatmap with HD and NF (https://inex.osekai.net/medals/Camera%20Shy/)
            "camera_shy" => fullCombo && Has("HD") && Has("NF"),

            // The Sum Of All Fears: Pass with a maximum combo exactly one below the theoretical maximum after mods (https://inex.osekai.net/medals/The%20Sum%20Of%20All%20Fears/)
            "the_sun_of_all_fears" => pass && context.Difficulty.MaxCombo > 1 && score.MaxCombo == context.Difficulty.MaxCombo - 1,

            // Hour Before The Dawn: PFC any difficulty of ginkiha - EOS (https://inex.osekai.net/medals/Hour%20Before%20The%20Dawn/)
            "hour_before_the_down" => fullCombo && Song("ginkiha", "EOS"),

            // Slow And Steady: PFC with HT and PF on a beatmap rated at least 3 stars after mods (https://inex.osekai.net/medals/Slow%20And%20Steady/)
            "slow_and_steady" => fullCombo && Has("HT") && Has("PF") && stars >= 3,

            // No Time To Spare: PFC with DT or NC on a beatmap whose total length is at most 30 seconds after mods (https://inex.osekai.net/medals/No%20Time%20To%20Spare/)
            "no_time_to_spare" => fullCombo && Fast() && beatmap.TotalLength / rate <= 30,

            // Sognare: Pass LeaF - Evanescent with HT (https://inex.osekai.net/medals/Sognare/)
            "sognare" => pass && Has("HT") && Song("LeaF", "Evanescent"),

            // Realtor Extraordinaire: PFC any difficulty of cYsmix - House With Legs with HR and either DT or NC (https://inex.osekai.net/medals/Realtor%20Extraordinaire/)
            "realtor_extraordinaire" => fullCombo && Fast() && Has("HR") && Song("cYsmix", "House With Legs"),

            // Impeccable: Pass with PF and either DT or NC on a beatmap rated at least 4 stars after mods (https://inex.osekai.net/medals/Impeccable/)
            "impeccable" => pass && Fast() && Has("PF") && stars >= 4,

            // Aeon: PFC with FL, HD and HT on a beatmap ranked by 2011, rated at least 4 stars and lasting at least 3 minutes after mods (https://inex.osekai.net/medals/Aeon/)
            "aeon" => fullCombo && Has("FL") && Has("HD") && Has("HT") && set?.RankedDate is { Year: <= 2011 } && beatmap.TotalLength / rate >= 180 && stars >= 4,

            // Quick Maths: Get exactly 34 misses on any difficulty of Function Phantom - Variable (https://inex.osekai.net/medals/Quick%20Maths/)
            "quick_maths" => misses == 34 && Song("Function Phantom", "Variable"),

            // Kaleidoscope: Pass The Flashbulb - DIDJ PVC [EX III] (2022237) with EZ, HT and at least 80% accuracy (https://inex.osekai.net/medals/Kaleidoscope/)
            "kaleidoscope" => pass && Has("EZ") && Has("HT") && beatmap.Id == 2022237 && score.Accuracy >= 0.8,

            // Valediction: Pass a_hisa - Alexithymia | Lupinus | Tokei no Heya to Seishin Sekai with at least 90% accuracy (https://inex.osekai.net/medals/Valediction/)
            "valediction" => pass && Song("a_hisa", "Alexithymia | Lupinus | Tokei no Heya to Seishin Sekai") && score.Accuracy >= 0.9,

            // Right On Time: Pass Kola Kid - timer with the score ending during the first minute of an hour (XX:00) (https://inex.osekai.net/medals/Right%20On%20Time/)
            "right_on_time" => pass && Song("Kola Kid", "timer") && score.EndedAt.UtcDateTime.Minute == 0,

            // Not Again: Pass ARForest - Regret with exactly one miss and at least 99% accuracy (https://inex.osekai.net/medals/Not%20Again/)
            "not_again" => pass && misses == 1 && score.Accuracy >= 0.99 && Song("ARForest", "Regret"),

            // Deliberation: PFC a Ranked, Approved or Loved beatmap with HT and at least 6 stars after mods (https://inex.osekai.net/medals/Deliberation/)
            "deliberation" => fullCombo && Has("HT") && (ranked || beatmap.Status == BeatmapOnlineStatus.Loved) && stars >= 6,

            // Clarity: Pass any difficulty of rrtyui's Camellia vs Akira Complex - Reality Distortion mapset (582089) (https://inex.osekai.net/medals/Clarity/)
            "clarity" => pass && beatmap.BeatmapSetId == 582089,

            // Autocreation: Pass a beatmap whose nonempty artist name exactly matches the mapset creator name (https://inex.osekai.net/medals/Autocreation/)
            "autocreation" => pass && set is not null && set.Artist.Length > 0 && string.Equals(set.Creator, set.Artist, StringComparison.Ordinal),

            // Value Your Identity: Pass with a maximum combo matching the last three digits of the user ID; use 1,000 for IDs ending in 000 (https://inex.osekai.net/medals/Value%20Your%20Identity/)
            "value_your_identity" => pass && score.MaxCombo == (score.UserId % 1000 == 0 ? 1000 : score.UserId % 1000),

            // By The Skin Of The Teeth: Pass with AC and an accuracy differing from its configured target by less than 0.01 percentage points (https://inex.osekai.net/medals/By%20The%20Skin%20Of%20The%20Teeth/)
            "by_the_skin_of_the_teeth" => pass && score.Mods.Any(mod => string.Equals(mod.Acronym, "AC", StringComparison.Ordinal)
                                                                        && mod.Settings.TryGetValue("minimum_accuracy", out object? value) && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, out double target)
                                                                        && Math.Abs(score.Accuracy - target) < 0.0001),

            // Meticulous Mayhem: Pass any beatmap with at least 15 distinct mods enabled (https://inex.osekai.net/medals/Meticulous%20Mayhem/)
            "meticulous_mayhem" => pass && mods.Count >= 15,
            _ => false
        };

        bool Has(string mod) => mods.Contains(mod);
        bool Fast() => Has("DT") || Has("NC");
        bool Song(string artist, string title) => set is not null && string.Equals(set.Artist, artist, StringComparison.Ordinal) && string.Equals(set.Title, title, StringComparison.Ordinal);
        bool Range(double value) => value >= rule.Minimum && (rule.Maximum == 0 || value < rule.Maximum);
    }

    private static bool RepeatedDigits(long score)
    {
        string digits = score.ToString(CultureInfo.InvariantCulture);
        return score > 100000 && digits.All(digit => digit == digits[0]);
    }

    private sealed record MedalRule(Achievement Definition, string Rule, int Mode, double Minimum, double Maximum, string? Mod);
}