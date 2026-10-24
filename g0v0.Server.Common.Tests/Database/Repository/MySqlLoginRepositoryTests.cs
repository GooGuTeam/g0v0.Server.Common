// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class MySqlLoginRepositoryTests : LoginRepositoryTestsBase
{
    protected override DbContext CreateContext()
        => new MysqlDbContext(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    protected override ILoginRepository CreateRepository(DbContext context)
        => new g0v0.Server.Common.Database.MySQL.Repository.LoginRepository((MysqlDbContext)context);
}