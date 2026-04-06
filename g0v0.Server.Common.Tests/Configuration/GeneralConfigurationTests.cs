// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;

using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration;

[TestFixture]
public class GeneralConfigurationTests
{
    private GeneralConfiguration _config = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new GeneralConfiguration();
    }

    [Test]
    public void MySqlConnectionString_ShouldContainAllParts()
    {
        string connStr = _config.MySqlConnectionString;

        Assert.That(connStr, Does.Contain($"Server={_config.MySqlHost}"));
        Assert.That(connStr, Does.Contain($"Port={_config.MySqlPort}"));
        Assert.That(connStr, Does.Contain($"Database={_config.MySqlDatabase}"));
        Assert.That(connStr, Does.Contain($"Uid={_config.MySqlUsername}"));
        Assert.That(connStr, Does.Contain($"Pwd={_config.MySqlPassword}"));
    }

    [Test]
    public void MySqlConnectionString_DefaultValue_ShouldBeCorrectFormat()
    {
        const string expected =
            "Server=127.0.0.1;Port=3306;Database=osu_api;Uid=osu_api;Pwd=password;";

        Assert.That(_config.MySqlConnectionString, Is.EqualTo(expected));
    }

    [Test]
    public void MySqlHost_WhenSet_ShouldUpdateConnectionString()
    {
        _config.MySqlHost = "192.168.1.100";

        Assert.That(_config.MySqlConnectionString, Does.Contain("Server=192.168.1.100"));
    }
}