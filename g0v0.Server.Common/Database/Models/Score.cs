// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a submitted score record.
/// </summary>
[Index(nameof(UserId), nameof(Mode), nameof(Pp), nameof(Id), Name = "idx_score_user_mode_pp")]
[Index(nameof(UserId), nameof(Mode), nameof(EndedAt), nameof(Id), Name = "idx_score_user_mode_date")]
[Index(nameof(BeatmapId), Name = "ix_scores_beatmap_id")]
[Index(nameof(Mode), Name = "ix_scores_gamemode")]
[Index(nameof(BeatmapChecksum), Name = "ix_scores_map_md5")]
[Index(nameof(UserId), Name = "ix_scores_user_id")]
[Table("scores")]
public class Score
{
    /// <summary>
    /// Gets or sets the score ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the beatmap ID.
    /// </summary>
    public int BeatmapId { get; set; }

    /// <summary>
    /// Gets or sets the score rank.
    /// </summary>
    public ScoreRank Rank { get; set; }

    /// <summary>
    /// Gets or sets the score type (as server identity).
    /// </summary>
    [StringLength(10)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the submitting user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the score accuracy.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the gameplay start timestamp.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the gameplay end timestamp.
    /// </summary>
    public DateTimeOffset EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum combo achieved.
    /// </summary>
    public int MaxCombo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a replay is available.
    /// </summary>
    public bool HasReplay { get; set; }

    /// <summary>
    /// Gets or sets the total score.
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// Gets or sets the total score without mods.
    /// </summary>
    public int TotalScoreWithoutMods { get; set; }

    /// <summary>
    /// Gets or sets the legacy total score.
    /// </summary>
    public long ClassicTotalScore { get; set; }

    /// <summary>
    /// Gets or sets the legacy total score without mods.
    /// </summary>
    public long ClassicTotalScoreWithoutMods { get; set; }

    /// <summary>
    /// Gets or sets hit result counts achieved by the score.
    /// </summary>
    public IDictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

    /// <summary>
    /// Gets or sets the maximum possible hit result counts.
    /// </summary>
    public IDictionary<HitResult, int> MaximumStatistics { get; set; } = new Dictionary<HitResult, int>();

    /// <summary>
    /// Gets or sets the mods applied to the score.
    /// </summary>
    public IList<APIMod> Mods { get; set; } = new List<APIMod>();

    /// <summary>
    /// Gets or sets the legacy ruleset ID.
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Gets or sets the beatmap checksum.
    /// </summary>
    [StringLength(32)]
    public string BeatmapChecksum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the score has been processed.
    /// </summary>
    public bool Processed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the score is ranked.
    /// </summary>
    public bool Ranked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the score passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the performance points awarded by the score.
    /// </summary>
    public double Pp { get; set; }

    /// <summary>
    /// Gets or sets the multiplayer room ID, if applicable.
    /// </summary>
    public int? RoomId { get; set; }

    /// <summary>
    /// Gets or sets the multiplayer playlist item ID, if applicable.
    /// </summary>
    public long? PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the score should be preserved from pruning.
    /// </summary>
    public bool Preserve { get; set; } = true;

    /// <summary>
    /// Gets or sets the client version that submitted the score.
    /// </summary>
    [StringLength(50)]
    public string ClientVersion { get; set; } = string.Empty;

    #region Compatible with v1

    // These accessors back the legacy v1 score columns. They are only exercised
    // by EF Core mapping/materialization (see ScoreConfig), so suppress the
    // "never used" inspection for the whole region.
    // ReSharper disable UnusedMember.Local
    private int N300
    {
        get => this.GetRequiredStatistic(HitResult.Great);
        set => this.SetRequiredStatistic(HitResult.Great, value);
    }

    private int N100
    {
        get => this.GetRequiredStatistic(HitResult.Ok);
        set => this.SetRequiredStatistic(HitResult.Ok, value);
    }

    private int N50
    {
        get => this.GetRequiredStatistic(HitResult.Meh);
        set => this.SetRequiredStatistic(HitResult.Meh, value);
    }

    private int NMiss
    {
        get => this.GetRequiredStatistic(HitResult.Miss);
        set => this.SetRequiredStatistic(HitResult.Miss, value);
    }

    private int NGeki
    {
        get => this.GetRequiredStatistic(HitResult.Perfect);
        set => this.SetRequiredStatistic(HitResult.Perfect, value);
    }

    private int NKatu
    {
        get => this.GetRequiredStatistic(HitResult.Good);
        set => this.SetRequiredStatistic(HitResult.Good, value);
    }

    private int? NLargeTickMiss
    {
        get => this.GetOptionalStatistic(HitResult.LargeTickMiss);
        set => this.SetOptionalStatistic(HitResult.LargeTickMiss, value);
    }

    private int? NLargeTickHit
    {
        get => this.GetOptionalStatistic(HitResult.LargeTickHit);
        set => this.SetOptionalStatistic(HitResult.LargeTickHit, value);
    }

    private int? NSliderTailHit
    {
        get => this.GetOptionalStatistic(HitResult.SliderTailHit);
        set => this.SetOptionalStatistic(HitResult.SliderTailHit, value);
    }

    private int? NSmallTickHit
    {
        get => this.GetOptionalStatistic(HitResult.SmallTickHit);
        set => this.SetOptionalStatistic(HitResult.SmallTickHit, value);
    }

    private int? NSmallTickMiss
    {
        get => this.GetOptionalStatistic(HitResult.SmallTickMiss);
        set => this.SetOptionalStatistic(HitResult.SmallTickMiss, value);
    }

    private int GetRequiredStatistic(HitResult result)
    {
        return this.Statistics.TryGetValue(result, out int value) ? value : 0;
    }

    private int? GetOptionalStatistic(HitResult result)
    {
        return this.Statistics.TryGetValue(result, out int value) ? value : null;
    }

    private void SetRequiredStatistic(HitResult result, int value)
    {
        this.Statistics[result] = value;
    }

    private void SetOptionalStatistic(HitResult result, int? value)
    {
        if (value.HasValue)
        {
            this.Statistics[result] = value.Value;
            return;
        }

        this.Statistics.Remove(result);
    }

    // ReSharper restore UnusedMember.Local
    #endregion
}