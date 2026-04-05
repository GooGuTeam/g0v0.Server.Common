// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;

using Newtonsoft.Json;

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
    public void MySqlHost_DefaultValue_ShouldBe_LocalHost()
    {
        Assert.That(_config.MySqlHost, Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void MySqlPort_DefaultValue_ShouldBe_3306()
    {
        Assert.That(_config.MySqlPort, Is.EqualTo("3306"));
    }

    [Test]
    public void MySqlDatabase_DefaultValue_ShouldBe_OsuApi()
    {
        Assert.That(_config.MySqlDatabase, Is.EqualTo("osu_api"));
    }

    [Test]
    public void MySqlUsername_DefaultValue_ShouldBe_OsuApi()
    {
        Assert.That(_config.MySqlUsername, Is.EqualTo("osu_api"));
    }

    [Test]
    public void MySqlPassword_DefaultValue_ShouldBe_Password()
    {
        Assert.That(_config.MySqlPassword, Is.EqualTo("password"));
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
    public void MySqlConnectionString_ShouldHaveJsonIgnoreAttribute()
    {
        var property = typeof(GeneralConfiguration).GetProperty(nameof(GeneralConfiguration.MySqlConnectionString))!;
        object? jsonIgnore = property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).FirstOrDefault();

        Assert.That(
            jsonIgnore,
            Is.Not.Null,
            "MySqlConnectionString should be decorated with [JsonIgnore] to prevent serialization.");
    }
}