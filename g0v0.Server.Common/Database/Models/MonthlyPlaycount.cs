// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents how often one user played during one calendar month, materialised in the
/// <c>monthly_playcounts</c> table.
/// </summary>
[Table("monthly_playcounts")]
[Index(nameof(UserId), Name = "ix_monthly_playcounts_user_id")]
[Index(nameof(Year), Name = "ix_monthly_playcounts_year")]
[Index(nameof(Month), Name = "ix_monthly_playcounts_month")]
public class MonthlyPlaycount
{
    /// <summary>
    /// Gets or sets the surrogate primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user the play count belongs to.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the calendar year of the bucket.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the calendar month of the bucket, from <c>1</c> to <c>12</c>.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the number of plays in the bucket.
    /// </summary>
    public int Count { get; set; }
}