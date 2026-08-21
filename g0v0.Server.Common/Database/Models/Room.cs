// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using OsuMatchType = osu.Game.Online.Rooms.MatchType;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a multiplayer room record.
/// </summary>
[Index(nameof(Name))]
[Index(nameof(Category))]
[Index(nameof(HostId))]
[Index(nameof(Id))]
[Table("rooms")]
public class Room
{
    /// <summary>
    /// Gets or sets the room ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the room name.
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the room category.
    /// </summary>
    public G0V0RoomCategory Category { get; set; } = G0V0RoomCategory.Normal;

    /// <summary>
    /// Gets or sets the room status.
    /// </summary>
    public RoomStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the match type.
    /// </summary>
    public OsuMatchType Type { get; set; }

    /// <summary>
    /// Gets or sets the room start timestamp.
    /// </summary>
    public DateTimeOffset? StartsAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the room end timestamp.
    /// </summary>
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of playable attempts.
    /// </summary>
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// Gets or sets the participant count.
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Gets or sets the associated chat channel ID.
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the room queue mode.
    /// </summary>
    public QueueMode QueueMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gameplay should automatically skip after all players complete.
    /// </summary>
    public bool AutoSkip { get; set; }

    /// <summary>
    /// Gets or sets the auto-start duration in seconds.
    /// </summary>
    public int AutoStartDuration { get; set; }

    /// <summary>
    /// Gets or sets the room host user ID.
    /// </summary>
    public int? HostId { get; set; }

    /// <summary>
    /// Gets or sets the optional room password.
    /// </summary>
    [StringLength(40)] // https://github.com/ppy/osu/blob/b1062f68a24b7d649a0053fab5b6ce15d1540b3b/osu.Game/Screens/OnlinePlay/Multiplayer/Match/MultiplayerMatchSettingsOverlay.cs#L251-L257
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of participants allowed in the room.
    /// </summary>
    public byte? MaxParticipants { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the room is a tournament room.
    /// </summary>
    public bool TournamentMode { get; set; }

    /// <summary>
    /// Gets or sets the room host.
    /// </summary>
    [ForeignKey(nameof(HostId))]
    public User? Host { get; set; }

    /// <summary>
    /// Gets or sets the playlists associated with the room.
    /// </summary>
    [InverseProperty(nameof(Playlist.Room))]
    public virtual ICollection<Playlist> Playlists { get; set; }
        = new List<Playlist>();
}