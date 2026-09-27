// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace g0v0.Server.Common.Database.PostgreSQL;

/// <inheritdoc />
public class PostgreSqlDbContextFactory
    : IDesignTimeDbContextFactory<PostgreSqlDbContext>
{
    /// <inheritdoc/>
    public PostgreSqlDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgreSqlDbContext> optionsBuilder = new();

        // Split on the first '=' only: connection strings contain '=' themselves.
        string? conn =
            args.FirstOrDefault(x => x.StartsWith("--conn=", StringComparison.Ordinal))?["--conn=".Length..]
            ?? args.SkipWhile(x => !string.Equals(x, "--conn", StringComparison.Ordinal)).Skip(1).FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("DB_CONN");

        if (string.IsNullOrEmpty(conn))
        {
            throw new InvalidOperationException(
                "connection string not provided. Pass it by --conn=your_connection_string");
        }

        optionsBuilder.UseNpgsql(conn);

        return new PostgreSqlDbContext(optionsBuilder.Options);
    }
}