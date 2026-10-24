// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// Provides PostgreSQL login persistence.
/// </summary>
/// <param name="context">
/// The request-scoped database context.
/// </param>
public class LoginRepository(PostgreSqlDbContext context)
    : g0v0.Server.Common.Database.Repository.LoginRepository<PostgreSqlDbContext>(context), IPostgreSqlRepository;