// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Configuration.Attributes;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration;

[TestFixture]
public class GameConfigTests
{
    [Test]
    public void GameConfiguration_ShouldUseGameConfigurationFile()
    {
        ConfigFileAttribute? attribute = typeof(GameConfig)
            .GetCustomAttributes(typeof(ConfigFileAttribute), inherit: false)
            .Single() as ConfigFileAttribute;

        Assert.That(attribute!.FileName, Is.EqualTo("game"));
    }

    [Test]
    public void FeatureFlags_ShouldBeReloadable()
    {
        string[] reloadableProperties = typeof(GameConfig)
            .GetProperties()
            .Where(property => property.GetCustomAttributes(typeof(ReloadableAttribute), inherit: false).Length > 0)
            .Select(property => property.Name)
            .ToArray();

        Assert.That(
            reloadableProperties,
            Is.EquivalentTo(
            [
                nameof(GameConfig.EnableRelax),
                nameof(GameConfig.EnableAutopilot),
                nameof(GameConfig.EnableAllBeatmapLeaderboard),
            ]));
    }

    [Test]
    public void DefaultValues_ShouldDisableOptionalGameplayFeatures()
    {
        GameConfig config = new();

        Assert.Multiple(() =>
        {
            Assert.That(config.EnableRelax, Is.False);
            Assert.That(config.EnableAutopilot, Is.False);
            Assert.That(config.EnableAllBeatmapLeaderboard, Is.False);
        });
    }
}