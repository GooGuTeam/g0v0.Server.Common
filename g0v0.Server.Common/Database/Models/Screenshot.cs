// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Stores screenshot ownership, content identity and access statistics.
/// </summary>
[Table("screenshots")]
[Index(nameof(Sha256Hash), Name = "ix_screenshots_sha256_hash")]
[Index(nameof(UserId), Name = "ix_screenshots_user_id")]
[Index(nameof(UserId), nameof(Sha256Hash), Name = "ix_screenshots_user_hash")]
[Index(nameof(Hits), nameof(Timestamp), Name = "ix_screenshots_hits_timestamp")]
public class Screenshot
{
    /// <summary>Gets or sets the screenshot ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the SHA-256 digest of the stored bytes.</summary>
    [Column("sha256_hash")]
    [StringLength(64)]
    public required string Sha256Hash { get; set; }

    /// <summary>Gets or sets the storage URL, which may expire.</summary>
    public required string Url { get; set; }

    /// <summary>Gets or sets the uploader ID.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the most recent upload time.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the share link view count.</summary>
    public int Hits { get; set; }

    /// <summary>Gets or sets when the share link was last accessed.</summary>
    public DateTimeOffset LastAccess { get; set; }
}