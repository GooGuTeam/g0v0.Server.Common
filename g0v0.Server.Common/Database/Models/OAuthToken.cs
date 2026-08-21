// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents an OAuth access/refresh token pair stored in the <c>oauth_tokens</c> table.
/// </summary>
[Table("oauth_tokens")]
[Index(nameof(UserId))]
[Index(nameof(ClientId))]
[Index(nameof(AccessToken), IsUnique = true)]
[Index(nameof(RefreshToken), IsUnique = true)]
[Index(nameof(ExpiresAt))]
[Index(nameof(RefreshTokenExpiresAt))]
public class OAuthToken
{
    /// <summary>
    /// Gets or sets the token record identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this token. This is a nullable field, as some tokens may not be associated with a user (e.g., client credentials flow).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client ID that issued the token.
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// Gets or sets the access token string.
    /// </summary>
    [StringLength(500)]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token string.
    /// </summary>
    [StringLength(500)]
    public required string RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    [StringLength(20)]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets the granted OAuth scope list.
    /// </summary>
    [StringLength(100)]
    public string Scope { get; set; } = "*";

    /// <summary>
    /// Gets or sets the access token expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the refresh token expiration timestamp.
    /// </summary>
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the record creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the related user entity.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}