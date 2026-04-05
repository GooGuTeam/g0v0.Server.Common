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

        Assert.DoesNotThrow(() => new ConfigurationManager<SimpleTestConfig>(_tempDir));
    }

    [Test]
    public void Constructor_WithConfigurationFileAttribute_ShouldUseAttributeFileName()
    {
        // AttributeTestConfig has [ConfigurationFile("attr_test_config.json")]
        WriteConfigFile("attr_test_config.json", new { Data = "world" });

        Assert.DoesNotThrow(() => new ConfigurationManager<AttributeTestConfig>(_tempDir));
    }

    #endregion

    #region Construction: value loading

    [Test]
    public void Value_AfterConstruction_ShouldContainDeserializedProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "loaded", Value = 42, ReloadableValue = "initial" });

        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        Assert.That(manager.Value.Name, Is.EqualTo("loaded"));
        Assert.That(manager.Value.Value, Is.EqualTo(42));
        Assert.That(manager.Value.ReloadableValue, Is.EqualTo("initial"));
    }

    [Test]
    public void Value_WithAttributeFileName_ShouldContainDeserializedProperties()
    {
        WriteConfigFile("attr_test_config.json", new { Data = "attribute-loaded" });

        var manager = new ConfigurationManager<AttributeTestConfig>(_tempDir);

        Assert.That(manager.Value.Data, Is.EqualTo("attribute-loaded"));
    }

    #endregion

    #region Construction: error cases

    [Test]
    public void Constructor_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => new ConfigurationManager<SimpleTestConfig>(_tempDir));
    }

    [Test]
    public void Constructor_WhenFileDoesNotExist_ExceptionMessageShouldContainFilePath()
    {
        var ex = Assert.Throws<FileNotFoundException>(() => new ConfigurationManager<SimpleTestConfig>(_tempDir));

        Assert.That(ex!.Message, Does.Contain("simple_test_config.json"));
    }

    [Test]
    public void Constructor_WhenJsonIsInvalid_ShouldThrowJsonReaderException()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "not { valid } json {{");

        Assert.Throws<JsonReaderException>(() => new ConfigurationManager<SimpleTestConfig>(_tempDir));
    }

    [Test]
    public void Constructor_WhenJsonIsNull_ShouldThrowInvalidDataException()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "null");

        Assert.Throws<InvalidDataException>(() => new ConfigurationManager<SimpleTestConfig>(_tempDir));
    }

    [Test]
    public void Constructor_WhenJsonIsEmptyObject_ShouldSucceedWithDefaults()
    {
        File.WriteAllText(Path.Combine(_configDir, "simple_test_config.json"), "{}");

        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        Assert.That(manager.Value, Is.Not.Null);
        Assert.That(manager.Value.Name, Is.EqualTo(string.Empty));
        Assert.That(manager.Value.Value, Is.EqualTo(0));
    }

    #endregion

    #region Reload

    [Test]
    public void Reload_WhenFileChanged_ShouldUpdateReloadableProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v1" });
        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v2" });
        manager.Reload();

        Assert.That(manager.Value.ReloadableValue, Is.EqualTo("v2"));
    }

    [Test]
    public void Reload_WhenFileChanged_ShouldNotUpdateNonReloadableProperties()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "original", Value = 1, ReloadableValue = "v1" });
        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "changed", Value = 99, ReloadableValue = "v2" });
        manager.Reload();

        Assert.That(manager.Value.Name, Is.EqualTo("original"));
        Assert.That(manager.Value.Value, Is.EqualTo(1));
    }

    [Test]
    public void Reload_WhenFileNotChanged_ShouldKeepSameValues()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "stable", Value = 5, ReloadableValue = "same" });
        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        manager.Reload();

        Assert.That(manager.Value.Name, Is.EqualTo("stable"));
        Assert.That(manager.Value.Value, Is.EqualTo(5));
        Assert.That(manager.Value.ReloadableValue, Is.EqualTo("same"));
    }

    [Test]
    public void Reload_WhenFileDisappears_ShouldThrowFileNotFoundException()
    {
        WriteConfigFile(
            "simple_test_config.json",
            new { Name = "x", Value = 0, ReloadableValue = "x" });
        var manager = new ConfigurationManager<SimpleTestConfig>(_tempDir);

        File.Delete(Path.Combine(_configDir, "simple_test_config.json"));

        Assert.Throws<FileNotFoundException>(manager.Reload);
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