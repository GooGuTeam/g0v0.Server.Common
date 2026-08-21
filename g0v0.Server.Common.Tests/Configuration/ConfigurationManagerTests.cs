// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Configuration.Attributes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration;

[TestFixture]
public class ConfigurationManagerTests
{
    private string _tempDir = null!;
    private string _configDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _configDir = Path.Combine(_tempDir, "config");
        Directory.CreateDirectory(_configDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Construction: file resolution

    [Test]
    public void Constructor_WithoutAttribute_ShouldResolveSnakeCaseFileName()
    {
        // SimpleTestConfig → simple_test_config.json
        WriteConfigFile("simple_test_config.json", new { Name = "hello", Value = 7, ReloadableValue = "x" });

        Assert.DoesNotThrow(() => _ = new ConfigurationManager(_tempDir));
    }

    [Test]
    public void Get_AfterConstruction_ShouldReturnDeserializedProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "loaded", Value = 42, ReloadableValue = "initial" });

        ConfigurationManager manager = new(_tempDir);
        SimpleTestConfig config = manager.Get<SimpleTestConfig>();

        Assert.That(config.Name, Is.EqualTo("loaded"));
        Assert.That(config.Value, Is.EqualTo(42));
        Assert.That(config.ReloadableValue, Is.EqualTo("initial"));
    }

    [Test]
    public void Get_WithAttributeFileName_ShouldReturnDeserializedProperties()
    {
        WriteConfigFile("attr_test_config.json", new { Data = "attribute-loaded" });

        ConfigurationManager manager = new(_tempDir);
        AttributeTestConfig config = manager.Get<AttributeTestConfig>();

        Assert.That(config.Data, Is.EqualTo("attribute-loaded"));
    }

    [Test]
    public void Get_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        ConfigurationManager manager = new(_tempDir);

        Assert.Throws<FileNotFoundException>(() => manager.Get<SimpleTestConfig>());
    }

    [Test]
    public void Get_WhenFileDoesNotExist_ExceptionMessageShouldContainFilePath()
    {
        ConfigurationManager manager = new(_tempDir);
        FileNotFoundException? ex = Assert.Throws<FileNotFoundException>(() => manager.Get<SimpleTestConfig>());

        Assert.That(ex!.Message, Does.Contain("simple_test_config.json"));
    }

    [Test]
    public void Get_WhenJsonIsInvalid_ShouldThrowJsonReaderException()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "not { valid } json {{");

        ConfigurationManager manager = new(_tempDir);

        Assert.Throws<JsonReaderException>(() => manager.Get<SimpleTestConfig>());
    }

    [Test]
    public void Get_WhenJsonIsNull_ShouldThrowInvalidDataException()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "null");

        ConfigurationManager manager = new(_tempDir);

        Assert.Throws<InvalidDataException>(() => manager.Get<SimpleTestConfig>());
    }

    [Test]
    public void Get_WhenJsonIsEmptyObject_ShouldSucceedWithDefaults()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "{}");

        ConfigurationManager manager = new(_tempDir);
        SimpleTestConfig config = manager.Get<SimpleTestConfig>();

        Assert.That(config, Is.Not.Null);
        Assert.That(config.Name, Is.EqualTo(string.Empty));
        Assert.That(config.Value, Is.EqualTo(0));
    }

    #endregion

    #region Reload

    [Test]
    public void Reload_WhenFileChanged_ShouldUpdateReloadableProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v1" });
        ConfigurationManager manager = new(_tempDir);

        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v2" });
        manager.Reload<SimpleTestConfig>();

        SimpleTestConfig config = manager.Get<SimpleTestConfig>();
        Assert.That(config.ReloadableValue, Is.EqualTo("v2"));
    }

    [Test]
    public void Reload_WhenFileChanged_ShouldNotUpdateNonReloadableProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v1" });
        ConfigurationManager manager = new(_tempDir);

        // Cache the original values first.
        SimpleTestConfig firstLoad = manager.Get<SimpleTestConfig>();
        Assert.That(firstLoad.Name, Is.EqualTo("original"));

        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "changed", Value = 99, ReloadableValue = "v2" });
        manager.Reload<SimpleTestConfig>();

        SimpleTestConfig config = manager.Get<SimpleTestConfig>();
        Assert.That(config.Name, Is.EqualTo("original"));
        Assert.That(config.Value, Is.EqualTo(1));
    }

    [Test]
    public void Reload_WhenFileNotChanged_ShouldKeepSameValues()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "stable", Value = 5, ReloadableValue = "same" });
        ConfigurationManager manager = new(_tempDir);

        manager.Reload<SimpleTestConfig>();

        SimpleTestConfig config = manager.Get<SimpleTestConfig>();
        Assert.That(config.Name, Is.EqualTo("stable"));
        Assert.That(config.Value, Is.EqualTo(5));
        Assert.That(config.ReloadableValue, Is.EqualTo("same"));
    }

    [Test]
    public void Reload_WhenFileDisappears_ShouldThrowFileNotFoundException()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "x", Value = 0, ReloadableValue = "x" });
        ConfigurationManager manager = new(_tempDir);

        File.Delete(Path.Combine(_configDir, "simple_test_config.json"));

        Assert.Throws<FileNotFoundException>(() => manager.Reload<SimpleTestConfig>());
    }

    #endregion

    #region Helpers

    private void WriteConfigFile(string fileName, object content)
    {
        File.WriteAllText(
            Path.Combine(_configDir, fileName),
            JsonConvert.SerializeObject(content));
    }

    #endregion

    #region Inner test configuration types

    /// <summary>
    /// A plain test config – filename resolves to <c>simple_test_config.json</c> via snake_case.
    /// </summary>
    public class SimpleTestConfig
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        [Reloadable]
        public string ReloadableValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// A config that specifies its filename explicitly via <see cref="ConfigurationFileAttribute"/>.
    /// </summary>
    [ConfigurationFile("attr_test_config.json")]
    public class AttributeTestConfig
    {
        public string Data { get; set; } = string.Empty;
    }

    #endregion
}