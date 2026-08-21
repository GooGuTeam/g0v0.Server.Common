// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a multiplayer room playlist item record.
/// </summary>
[Table("playlists")]
[Index(nameof(RoomId))]
public class Playlist
{
    /// <summary>
    /// Gets or sets the public playlist item ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the room ID.
    /// </summary>
    public long RoomId { get; set; }

    /// <summary>
    /// Gets or sets the beatmap ID.
    /// </summary>
    public int BeatmapId { get; set; }

    /// <summary>
    /// Gets or sets the playlist creation timestamp.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the ruleset ID.
    /// </summary>
    public int RulesetId { get; set; }

    /// <summary>
    /// Gets or sets the mods participants may optionally apply.
    /// </summary>
    public IList<APIMod> AllowedMods { get; set; } = new List<APIMod>();

    /// <summary>
    /// Gets or sets the mods every participant must apply.
    /// </summary>
    public IList<APIMod> RequiredMods { get; set; } = new List<APIMod>();

    /// <summary>
    /// Gets or sets a value indicating whether players may choose their own beatmap difficulty, ruleset, and mods.
    /// </summary>
    public bool Freestyle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this playlist item has expired.
    /// </summary>
    public bool Expired { get; set; }

    /// <summary>
    /// Gets or sets the playlist item owner user ID.
    /// </summary>
    public int OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the playlist item order.
    /// </summary>
    public ushort PlaylistOrder { get; set; }

    /// <summary>
    /// Gets or sets when this playlist item was played.
    /// </summary>
    public DateTimeOffset? PlayedAt { get; set; }

    /// <summary>
    /// Gets or sets the score ordering condition for this playlist item.
    /// </summary>
    public WinCondition? WinCondition { get; set; }

    /// <summary>
    /// Gets or sets the beatmap associated with this playlist item.
    /// </summary>
    [ForeignKey(nameof(BeatmapId))]
    public Beatmap? Beatmap { get; set; }

    /// <summary>
    /// Gets or sets the room associated with this playlist item.
    /// </summary>
    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    /// <summary>
    /// Gets or sets the user that owns this playlist item.
    /// </summary>
    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }

    /// <summary>
    /// Gets or sets the playlist last update timestamp.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Converts this playlist item into the osu! multiplayer playlist item representation.
    /// </summary>
    /// <returns>The converted <see cref="MultiplayerPlaylistItem"/>.</returns>
    public MultiplayerPlaylistItem ToMultiplayerPlaylistItem()
    {
        return new MultiplayerPlaylistItem
        {
            ID = Id,
            OwnerID = OwnerId,
            BeatmapID = BeatmapId,
            BeatmapChecksum = Beatmap?.Checksum ?? string.Empty,
            RulesetID = RulesetId,
            RequiredMods = RequiredMods,
            AllowedMods = AllowedMods,
            Freestyle = Freestyle,
            Expired = Expired,
            PlayedAt = PlayedAt,
            PlaylistOrder = PlaylistOrder,
            StarRating = Beatmap?.DifficultyRating ?? 0,
            WinCondition = WinCondition ?? osu.Game.Online.Multiplayer.WinCondition.Score,
        };
    }
}