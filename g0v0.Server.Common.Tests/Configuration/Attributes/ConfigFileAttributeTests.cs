// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration.Attributes;

[TestFixture]
public class ConfigFileAttributeTests
{
    [Test]
    public void FileName_ShouldReturnValuePassedToConstructor()
    {
        ConfigFileAttribute attribute = new("my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("my_config.json"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithoutExtension()
    {
        ConfigFileAttribute attribute = new(fileName: "general");

        Assert.That(attribute.FileName, Is.EqualTo("general"));
    }

    [Test]
    public void FileName_ShouldPreserveExactString_WithPathSeparators()
    {
        ConfigFileAttribute attribute = new(fileName: "sub/my_config.json");

        Assert.That(attribute.FileName, Is.EqualTo("sub/my_config.json"));
    }

    [Test]
    public void Attribute_ShouldOnlyTargetClasses()
    {
        AttributeUsageAttribute usageAttr = typeof(ConfigFileAttribute)
            .GetCustomAttributes(attributeType: typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usageAttr.ValidOn, Is.EqualTo(AttributeTargets.Class));
    }

    [Test]
    public void Attribute_CanBeAppliedToClass()
    {
        ConfigFileAttribute? attr = typeof(SampleClassWithConfigFile)
            .GetCustomAttributes(attributeType: typeof(ConfigFileAttribute), inherit: false)
            .Cast<ConfigFileAttribute>()
            .SingleOrDefault();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.FileName, Is.EqualTo("sample.json"));
    }

    [ConfigFile("sample.json")]
    private sealed class SampleClassWithConfigFile
    {
    }
}