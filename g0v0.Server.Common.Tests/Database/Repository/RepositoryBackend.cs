// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// The database backend a backend-parameterised repository fixture runs against.
/// </summary>
public enum RepositoryBackend
{
    /// <summary>The legacy MySQL schema.</summary>
    MySql,

    /// <summary>The v2 PostgreSQL schema.</summary>
    PostgreSql,
}