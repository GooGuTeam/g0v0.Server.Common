// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlLoginRepositoryTests : LoginRepositoryTestsBase
{
    protected override DbContext CreateContext()
        => new PostgreSqlDbContext(new DbContextOptionsBuilder<PostgreSqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    protected override ILoginRepository CreateRepository(DbContext context)
        => new g0v0.Server.Common.Database.PostgreSQL.Repository.LoginRepository((PostgreSqlDbContext)context);
}