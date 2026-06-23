// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Configuration.Attributes;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration;

[TestFixture]
public class GameConfigurationTests
{
    [Test]
    public void GameConfiguration_ShouldUseGameConfigurationFile()
    {
        var attribute = typeof(GameConfiguration)
            .GetCustomAttributes(typeof(ConfigurationFileAttribute), inherit: false)
            .Single() as ConfigurationFileAttribute;

        Assert.That(attribute!.FileName, Is.EqualTo("game"));
    }

    [Test]
    public void FeatureFlags_ShouldBeReloadable()
    {
        string[] reloadableProperties = typeof(GameConfiguration)
            .GetProperties()
            .Where(property => property.GetCustomAttributes(typeof(ReloadableAttribute), inherit: false).Length > 0)
            .Select(property => property.Name)
            .ToArray();

        Assert.That(
            reloadableProperties,
            Is.EquivalentTo(new[]
            {
                nameof(GameConfiguration.EnableRelax),
                nameof(GameConfiguration.EnableAutopilot),
                nameof(GameConfiguration.EnableAllBeatmapLeaderboard),
            }));
    }

    [Test]
    public void DefaultValues_ShouldDisableOptionalGameplayFeatures()
    {
        var configuration = new GameConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.EnableRelax, Is.False);
            Assert.That(configuration.EnableAutopilot, Is.False);
            Assert.That(configuration.EnableAllBeatmapLeaderboard, Is.False);
        });
    }
}