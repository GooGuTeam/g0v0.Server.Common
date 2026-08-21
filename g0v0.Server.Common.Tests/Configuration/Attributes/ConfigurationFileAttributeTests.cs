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
        ConfigurationFileAttribute attribute = new("my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("my_config.json"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithoutExtension()
    {
        ConfigurationFileAttribute attribute = new(fileName: "general");

        Assert.That(attribute.FileName, Is.EqualTo("general"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithPathSeparators()
    {
        ConfigurationFileAttribute attribute = new(fileName: "sub/my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("sub/my_config.json"));
    }

    [Test]
    public void Attribute_ShouldOnlyTargetClasses()
    {
        AttributeUsageAttribute usageAttr = typeof(ConfigurationFileAttribute)
            .GetCustomAttributes(attributeType: typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usageAttr.ValidOn, Is.EqualTo(AttributeTargets.Class));
    }

    [Test]
    public void Attribute_CanBeAppliedToClass()
    {
        ConfigurationFileAttribute? attr = typeof(SampleClassWithConfigFile)
            .GetCustomAttributes(attributeType: typeof(ConfigurationFileAttribute), inherit: false)
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