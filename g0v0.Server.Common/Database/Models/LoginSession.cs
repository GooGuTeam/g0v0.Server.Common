// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a login session and its verification state.
/// </summary>
[Table("login_sessions")]
[Index(nameof(UserId))]
[Index(nameof(DeviceId))]
[Index(nameof(TokenId))]
public class LoginSession
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
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC verification time.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration time.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the trusted device identifier.
    /// </summary>
    public int? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth token identifier.
    /// </summary>
    public int? TokenId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this login uses a new device.
    /// </summary>
    public bool IsNewDevice { get; set; }

    /// <summary>
    /// Gets or sets the device hash.
    /// </summary>
    [Column(TypeName = "text")]
    public string? DeviceHash { get; set; }

    /// <summary>
    /// Gets or sets the verification method (totp or mail).
    /// </summary>
    [StringLength(20)]
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// Gets or sets the related user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the trusted device.
    /// </summary>
    [ForeignKey(nameof(DeviceId))]
    public TrustedDevice? Device { get; set; }

    /// <summary>
    /// Gets or sets the related OAuth token.
    /// </summary>
    [ForeignKey(nameof(TokenId))]
    public OAuthToken? Token { get; set; }
}