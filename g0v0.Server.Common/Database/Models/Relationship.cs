// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a user relationship record in the <c>relationship</c> table.
/// Tracks follow, friend, and block relationships between users.
/// </summary>
[Table("relationship")]
[Index(nameof(UserId))]
[Index(nameof(TargetId))]
public class Relationship
{
    /// <summary>
    /// Gets or sets the internal numeric identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the relationship.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the target user ID of the relationship.
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// Gets or sets the type of the relationship.
    /// </summary>
    public RelationshipType Type { get; set; } = RelationshipType.Follow;

    /// <summary>
    /// Gets or sets the user who initiated the relationship.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the target user of the relationship.
    /// </summary>
    [ForeignKey(nameof(TargetId))]
    public User? Target { get; set; }
}