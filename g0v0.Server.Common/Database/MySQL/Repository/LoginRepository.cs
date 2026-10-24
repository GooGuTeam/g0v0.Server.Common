// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// Provides MySQL login persistence.
/// </summary>
/// <param name="context">
/// The request-scoped database context.
/// </param>
public class LoginRepository(MysqlDbContext context)
    : g0v0.Server.Common.Database.Repository.LoginRepository<MysqlDbContext>(context), IMySqlRepository;