// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration.Attributes;

[TestFixture]
public class ReloadableAttributeTests
{
    [Test]
    public void Attribute_ShouldOnlyTargetProperties()
    {
        AttributeUsageAttribute usageAttr = typeof(ReloadableAttribute)
            .GetCustomAttributes(attributeType: typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usageAttr.ValidOn, Is.EqualTo(AttributeTargets.Property));
    }

    [Test]
    public void Attribute_CanBeAppliedToProperty()
    {
        System.Reflection.PropertyInfo property = typeof(SampleClassWithReloadable).GetProperty(nameof(SampleClassWithReloadable.ReloadableProp))!;
        ReloadableAttribute? attr = property.GetCustomAttributes(attributeType: typeof(ReloadableAttribute), inherit: false)
            .Cast<ReloadableAttribute>()
            .SingleOrDefault();

        Assert.That(attr, Is.Not.Null);
    }

    [Test]
    public void Attribute_IsNotAppliedToNonReloadableProperty()
    {
        System.Reflection.PropertyInfo property = typeof(SampleClassWithReloadable).GetProperty(nameof(SampleClassWithReloadable.NormalProp))!;
        ReloadableAttribute? attr = property.GetCustomAttributes(attributeType: typeof(ReloadableAttribute), inherit: false)
            .Cast<ReloadableAttribute>()
            .SingleOrDefault();

        Assert.That(attr, Is.Null);
    }

    private sealed class SampleClassWithReloadable
    {
        [Reloadable]
        public string ReloadableProp { get; set; } = string.Empty;

        public string NormalProp { get; set; } = string.Empty;
    }
}