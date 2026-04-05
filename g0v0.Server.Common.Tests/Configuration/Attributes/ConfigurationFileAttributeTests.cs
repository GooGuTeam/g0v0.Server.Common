// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration.Attributes;

[TestFixture]
public class ConfigurationFileAttributeTests
{
    [Test]
    public void FileName_ShouldReturnValuePassedToConstructor()
    {
        var attribute = new ConfigurationFileAttribute("my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("my_config.json"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithoutExtension()
    {
        var attribute = new ConfigurationFileAttribute("general");

        Assert.That(attribute.FileName, Is.EqualTo("general"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithPathSeparators()
    {
        var attribute = new ConfigurationFileAttribute("sub/my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("sub/my_config.json"));
    }

    [Test]
    public void Attribute_ShouldOnlyTargetClasses()
    {
        var usageAttr = typeof(ConfigurationFileAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usageAttr.ValidOn, Is.EqualTo(AttributeTargets.Class));
    }

    [Test]
    public void Attribute_CanBeAppliedToClass()
    {
        var attr = typeof(SampleClassWithConfigFile)
            .GetCustomAttributes(typeof(ConfigurationFileAttribute), false)
            .Cast<ConfigurationFileAttribute>()
            .SingleOrDefault();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.FileName, Is.EqualTo("sample.json"));
    }

    [ConfigurationFile("sample.json")]
    private sealed class SampleClassWithConfigFile
    {
    }
}