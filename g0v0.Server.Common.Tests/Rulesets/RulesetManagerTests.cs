// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Rulesets;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Rulesets;

[TestFixture]
public class RulesetManagerTests
{
    private string _tempDir = null!;
    private string _rulesetsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _rulesetsDir = Path.Combine(_tempDir, "rulesets");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Constructor: without official rulesets

    [Test]
    public void Constructor_WithoutOfficial_RulesetsDirDoesNotExist_ShouldHaveEmptyCollection()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    [Test]
    public void Constructor_WithoutOfficial_RulesetsDirExistsButEmpty_ShouldHaveEmptyCollection()
    {
        Directory.CreateDirectory(_rulesetsDir);

        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    #endregion

    #region Constructor: with IPathProvider

    #endregion

    #region GetRuleset: by legacy ID (empty manager)

    [Test]
    public void GetRuleset_ById_WhenManagerIsEmpty_ShouldThrowArgumentException()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        ArgumentException? ex = Assert.Throws<ArgumentException>(() => manager.GetRuleset(0));

        Assert.That(ex!.ParamName, Is.EqualTo("rulesetId"));
    }

    [Test]
    public void GetRuleset_ById_WhenManagerIsEmpty_ExceptionMessageShouldContainRulesetId()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        ArgumentException? ex = Assert.Throws<ArgumentException>(() => manager.GetRuleset(999));

        Assert.That(ex!.Message, Does.Contain("rulesetId"));
    }

    #endregion

    #region GetRuleset: by short name (empty manager)

    [Test]
    public void GetRuleset_ByName_WhenManagerIsEmpty_ShouldThrowArgumentException()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        ArgumentException? ex = Assert.Throws<ArgumentException>(() => manager.GetRuleset("osu"));

        Assert.That(ex!.ParamName, Is.EqualTo("shortName"));
    }

    [Test]
    public void GetRuleset_ByName_WhenManagerIsEmpty_ExceptionMessageShouldContainShortName()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        ArgumentException? ex = Assert.Throws<ArgumentException>(() => manager.GetRuleset("nonexistent"));

        Assert.That(ex!.Message, Does.Contain("shortName"));
    }

    #endregion

    #region GetAllRulesets

    [Test]
    public void GetAllRulesets_WhenManagerIsEmpty_ShouldReturnEmptyCollection()
    {
        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    #endregion

    #region Loading from disk with custom rulesets

    [Test]
    public void LoadFromDisk_WithNonRulesetDllInDirectory_ShouldIgnoreNonMatchingFiles()
    {
        Directory.CreateDirectory(_rulesetsDir);

        // Create a non-matching DLL (doesn't start with "osu.Game.Rulesets.").
        File.WriteAllText(Path.Combine(_rulesetsDir, "MyCustom.dll"), string.Empty);

        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    [Test]
    public void LoadFromDisk_WithTestDllInDirectory_ShouldSkipTestAssemblies()
    {
        Directory.CreateDirectory(_rulesetsDir);

        // Although this matches the prefix, it contains "Tests" so it should be skipped.
        File.WriteAllText(
            Path.Combine(_rulesetsDir, "osu.Game.Rulesets.MyRuleset.Tests.dll"), string.Empty);

        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    [Test]
    public void LoadFromDisk_InvalidDll_ShouldNotThrowAndRemainEmpty()
    {
        Directory.CreateDirectory(_rulesetsDir);

        // Write a file matching the prefix pattern but containing invalid IL.
        // Assembly.LoadFrom will throw a BadImageFormatException which is caught internally.
        File.WriteAllBytes(
            Path.Combine(_rulesetsDir, "osu.Game.Rulesets.Invalid.dll"),
            [0x00, 0x01, 0x02, 0x03]);

        RulesetManager manager = new(_tempDir, includeOfficial: false);

        Assert.That(manager.GetAllRulesets(), Is.Empty);
    }

    #endregion

    #region Helper types

    /// <summary>
    /// A minimal <see cref="IPathProvider"/> implementation for testing purposes.
    /// Returns the provided base path.
    /// </summary>
    private sealed class TestPathProvider : IPathProvider
    {
        public TestPathProvider(string basePath)
        {
            GetBasePathResult = basePath;
        }

        public string GetBasePathResult { get; }

        public string GetBasePath() => GetBasePathResult;
    }

    #endregion
}