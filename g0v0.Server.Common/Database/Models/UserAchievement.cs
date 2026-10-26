// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>Records a medal unlocked by a user.</summary>
[Table("user_achievements")]
[PrimaryKey(nameof(UserId), nameof(AchievementId))]
public class UserAchievement
{
    /// <summary>Gets or sets the user ID.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the shared medal ID.</summary>
    public int AchievementId { get; set; }

    /// <summary>Gets or sets the first unlock time.</summary>
    public DateTimeOffset AchievedAt { get; set; }

    /// <summary>Gets or sets the owner.</summary>
    public User? User { get; set; }
}