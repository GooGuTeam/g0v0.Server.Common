// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a user record in the <c>users</c> table.
/// </summary>
/// <remarks>
/// <para>
/// The model mirrors the user table of the osu!lazer API (<c>lazer_users</c> in the legacy
/// schema, see the lazer API migrations). Only the columns this server reads or writes are
/// mapped: the legacy table declares a further set of <c>NOT NULL</c> columns without a database
/// default (<c>priv</c>, <c>is_admin</c>, <c>max_blocks</c>, ...) that the MySQL mapping supplies
/// as shadow properties, so an insert keeps the row valid for the lazer API.
/// </para>
/// <para>
/// The two ruleset preferences are plain integers on both backends. The legacy MySQL table stores
/// them as native <c>gamemode</c> enum columns, so the MySQL mapping converts between the number
/// and the enum label; <see cref="Score.Mode"/> follows the same convention.
/// </para>
/// </remarks>
[Table("users")]
[Index(nameof(CountryCode))]
[Index(nameof(Username), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class User
{
    /// <summary>
    /// Gets or sets the internal numeric identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the avatar image URL.
    /// </summary>
    public string AvatarUrl { get; set; } = "https://lazer.g0v0.top/default.jpg";

    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    [StringLength(2)]
    public string CountryCode { get; set; } = "CN";

    /// <summary>
    /// Gets or sets a value indicating whether the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the account is a bot.
    /// </summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a supporter.
    /// </summary>
    public bool IsSupporter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is currently online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the last known visit timestamp.
    /// </summary>
    public DateTimeOffset? LastVisit { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether private messages are restricted to friends.
    /// </summary>
    public bool PmFriendsOnly { get; set; }

    /// <summary>
    /// Gets or sets the optional profile accent color.
    /// </summary>
    [StringLength(1 + 6)]
    public string? ProfileColour { get; set; }

    /// <summary>
    /// Gets or sets the unique username.
    /// </summary>
    [StringLength(32)]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the bcrypt password hash used by the stable client login flow.
    /// </summary>
    [StringLength(60)]
    public string PwBcrypt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique email address of the account.
    /// </summary>
    /// <remarks>
    /// The legacy schema declares the column as <c>NOT NULL</c>; it is optional here because an
    /// account created before registration stored one cannot be given one retroactively. The
    /// unique index tolerates several <see langword="null"/> values on both backends and
    /// registration always writes an address.
    /// </remarks>
    [StringLength(254)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the account creation timestamp.
    /// </summary>
    public DateTimeOffset JoinDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the support tier of the account.
    /// </summary>
    public int SupportLevel { get; set; }

    /// <summary>
    /// Gets or sets the ruleset ID the user last selected.
    /// </summary>
    /// <remarks>
    /// The value uses the same encoding as <see cref="Score.Mode"/>: 0-3 are the base rulesets,
    /// 4-7 the relax and autopilot variants, and 10-14 the g0v0 custom rulesets. The legacy MySQL
    /// column is a <c>gamemode</c> enum that stores these as labels.
    /// </remarks>
    public int Playmode { get; set; }

    /// <summary>
    /// Gets or sets the g0v0-specific ruleset preference, the counterpart of
    /// <see cref="Playmode"/> that the lazer API reads for its custom rulesets.
    /// </summary>
    [Column("g0v0_playmode")]
    public int G0V0Playmode { get; set; }
}