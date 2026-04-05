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
        var usageAttr = typeof(ReloadableAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usageAttr.ValidOn, Is.EqualTo(AttributeTargets.Property));
    }

    [Test]
    public void Attribute_CanBeAppliedToProperty()
    {
        var property = typeof(SampleClassWithReloadable).GetProperty(nameof(SampleClassWithReloadable.ReloadableProp))!;
        var attr = property.GetCustomAttributes(typeof(ReloadableAttribute), false)
            .Cast<ReloadableAttribute>()
            .SingleOrDefault();

        Assert.That(attr, Is.Not.Null);
    }

    [Test]
    public void Attribute_IsNotAppliedToNonReloadableProperty()
    {
        var property = typeof(SampleClassWithReloadable).GetProperty(nameof(SampleClassWithReloadable.NormalProp))!;
        var attr = property.GetCustomAttributes(typeof(ReloadableAttribute), false)
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