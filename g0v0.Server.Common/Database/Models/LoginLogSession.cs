// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Records a login attempt, matching the legacy UserLoginLog definition.
/// </summary>
[Table("user_login_log")]
[Index(nameof(UserId))]
[Index(nameof(IpAddress))]
public class LoginLogSession
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
    public required string IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the UTC login time.
    /// </summary>
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    [StringLength(2)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    [StringLength(100)]
    public string? CountryName { get; set; }

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    [StringLength(100)]
    public string? CityName { get; set; }

    /// <summary>
    /// Gets or sets the latitude as text.
    /// </summary>
    [StringLength(20)]
    public string? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude as text.
    /// </summary>
    [StringLength(20)]
    public string? Longitude { get; set; }

    /// <summary>
    /// Gets or sets the time zone.
    /// </summary>
    [StringLength(50)]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the autonomous system number.
    /// </summary>
    public int? Asn { get; set; }

    /// <summary>
    /// Gets or sets the organization name.
    /// </summary>
    [StringLength(200)]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether login succeeded.
    /// </summary>
    public bool LoginSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the login method.
    /// </summary>
    [StringLength(50)]
    public required string LoginMethod { get; set; }

    /// <summary>
    /// Gets or sets the additional login information.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}