// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a score upload token record.
/// </summary>
[Index(nameof(UserId), nameof(PlaylistItemId), Name = "idx_user_playlist")]
[Index(nameof(PlaylistItemId), nameof(RoomId), Name = "idx_playlist_room")]
[Table("score_tokens")]
public class ScoreToken
{
    /// <summary>
    /// Gets or sets the internal numeric identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the score ID associated with this token, if the score has been created.
    /// </summary>
    public long? ScoreId { get; set; }

    /// <summary>
    /// Gets or sets the ruleset ID used for the score.
    /// </summary>
    public int RulesetId { get; set; }

    /// <summary>
    /// Gets or sets the user ID that owns this score token.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the beatmap ID associated with this score token.
    /// </summary>
    public int BeatmapId { get; set; }

    /// <summary>
    /// Gets or sets the multiplayer room ID associated with this score token, if any.
    /// </summary>
    public int? RoomId { get; set; }

    /// <summary>
    /// Gets or sets the playlist item ID associated with this score token, if any.
    /// </summary>
    public int? PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets the token creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the token last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the client version that created the token.
    /// </summary>
    [StringLength(50)]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user that owns this score token.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the beatmap associated with this score token.
    /// </summary>
    [ForeignKey(nameof(BeatmapId))]
    public Beatmap? Beatmap { get; set; }

    /// <summary>
    /// Gets or sets the score associated with this token.
    /// </summary>
    [ForeignKey(nameof(ScoreId))]
    public Score? Score { get; set; }
}