// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a trusted login device.
/// </summary>
[Table("trusted_devices")]
[Index(nameof(UserId))]
public class TrustedDevice
{
    /// <summary>
    /// Gets or sets the record identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the IPv4 or IPv6 address.
    /// </summary>
    [StringLength(45)]
    public string IpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public required string UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the client type (web, lazer, or stable).
    /// </summary>
    [StringLength(10)]
    public string ClientType { get; set; } = "web";

    /// <summary>
    /// Gets or sets the device hash.
    /// </summary>
    [StringLength(64)]
    public string? DeviceHash { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC last-used time.
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC expiration time, if present in the legacy record.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the related user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the sessions associated with this device.
    /// </summary>
    public ICollection<LoginSession> Sessions { get; set; } = new List<LoginSession>();
}