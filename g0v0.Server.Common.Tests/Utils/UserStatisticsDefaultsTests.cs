// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Utils;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Utils;

public class UserStatisticsDefaultsTests
{
    [Test]
    public void CreatesOnlyTheBaseRulesetsWhenTheExtraModesAreDisabled()
    {
        int[] expected = [0, 1, 2, 3];

        IReadOnlyList<int> modes = UserStatisticsDefaults.GetModes(new GameConfig());

        Assert.That(modes, Is.EqualTo(expected));
    }

    [Test]
    public void AddsRelaxAndAutopilotModesWhenTheyAreEnabled()
    {
        GameConfig config = new() { EnableRelax = true, EnableAutopilot = true };

        IReadOnlyList<int> modes = UserStatisticsDefaults.GetModes(config);

        // Relax owns 4, 6 and 7 (osu!, taiko and catch); autopilot owns 5.
        int[] expected = [0, 1, 2, 3, 4, 5, 6, 7];
        Assert.That(modes, Is.EqualTo(expected));
    }

    [Test]
    public void AddsEachExtraModeOnlyWhenItsOwnFlagIsEnabled()
    {
        int[] relaxModes = [0, 1, 2, 3, 4, 6, 7];
        int[] autopilotModes = [0, 1, 2, 3, 5];

        Assert.Multiple(() =>
        {
            Assert.That(
                UserStatisticsDefaults.GetModes(new GameConfig { EnableRelax = true }),
                Is.EqualTo(relaxModes));
            Assert.That(
                UserStatisticsDefaults.GetModes(new GameConfig { EnableAutopilot = true }),
                Is.EqualTo(autopilotModes));
        });
    }
}