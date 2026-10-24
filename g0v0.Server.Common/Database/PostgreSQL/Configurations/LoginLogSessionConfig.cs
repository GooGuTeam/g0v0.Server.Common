// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures <see cref="LoginLogSession"/> for PostgreSQL.
/// </summary>
public class LoginLogSessionConfig : IEntityTypeConfiguration<LoginLogSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LoginLogSession> builder)
    {
        builder.ToTable("user_login_log");
    }
}