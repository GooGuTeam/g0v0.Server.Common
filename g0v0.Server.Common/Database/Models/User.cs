// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a user record in the <c>users</c> table.
/// </summary>
[Table("users")]
[Index(nameof(CountryCode))]
[Index(nameof(Username), IsUnique = true)]
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
}