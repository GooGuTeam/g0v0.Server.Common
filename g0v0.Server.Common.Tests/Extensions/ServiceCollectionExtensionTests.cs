// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionTests
{
    [Test]
    public void AddRepositories_WithLegacyDatabase_ShouldRegisterMySqlImplementations()
    {
        var services = new ServiceCollection();

        services.AddRepositories(useLegacyDatabase: true);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(
            descriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Should register the MySQL repository implementation.");
    }

    [Test]
    public void AddRepositories_WithPostgresDatabase_ShouldRegisterPostgresImplementations()
    {
        var services = new ServiceCollection();

        services.AddRepositories(useLegacyDatabase: false);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(
            descriptor!.ImplementationType!.FullName,
            Does.Contain("PostgreSQL"),
            "Should register the PostgreSQL repository implementation.");
    }

    [Test]
    public void AddRepositories_DefaultParameter_ShouldUseLegacyDatabase()
    {
        var services = new ServiceCollection();

        services.AddRepositories();

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(
            descriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Default should register MySQL (legacy) implementation.");
    }

    [Test]
    public void AddRepositories_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddRepositories();

        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddRepositories_ShouldRegisterAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRepositories(useLegacyDatabase: true);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }
}
