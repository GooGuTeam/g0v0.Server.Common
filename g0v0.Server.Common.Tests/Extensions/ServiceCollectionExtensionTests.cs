// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Communication;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Extensions;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Http;
using g0v0.Server.Common.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;

namespace g0v0.Server.Common.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionTests
{
    [Test]
    public void AddRepositories_WithLegacyDatabase_ShouldRegisterMySqlImplementations()
    {
        ServiceCollection services = new();

        services.AddRepositories(useLegacyDatabase: true);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));
        ServiceDescriptor? beatmapDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IBeatmapRepository));
        ServiceDescriptor? scoreDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IScoreRepository));
        ServiceDescriptor? userDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IUserRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(
            descriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Should register the MySQL repository implementation.");
        Assert.That(beatmapDescriptor, Is.Not.Null);
        Assert.That(
            beatmapDescriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Should register the MySQL beatmap repository implementation.");
        Assert.That(scoreDescriptor, Is.Not.Null);
        Assert.That(
            scoreDescriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Should register the MySQL score repository implementation.");
        Assert.That(userDescriptor, Is.Not.Null);
        Assert.That(
            userDescriptor!.ImplementationType!.FullName,
            Does.Contain("MySQL"),
            "Should register the MySQL user repository implementation.");
    }

    [Test]
    public void AddRepositories_WithPostgresDatabase_ShouldRegisterPostgresImplementations()
    {
        ServiceCollection services = new();

        services.AddRepositories(useLegacyDatabase: false);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));
        ServiceDescriptor? beatmapDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IBeatmapRepository));
        ServiceDescriptor? scoreDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IScoreRepository));
        ServiceDescriptor? userDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IUserRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(
            descriptor!.ImplementationType!.FullName,
            Does.Contain("PostgreSQL"),
            "Should register the PostgreSQL repository implementation.");
        Assert.That(beatmapDescriptor, Is.Not.Null);
        Assert.That(
            beatmapDescriptor!.ImplementationType!.FullName,
            Does.Contain("PostgreSQL"),
            "Should register the PostgreSQL beatmap repository implementation.");
        Assert.That(scoreDescriptor, Is.Not.Null);
        Assert.That(
            scoreDescriptor!.ImplementationType!.FullName,
            Does.Contain("PostgreSQL"),
            "Should register the PostgreSQL score repository implementation.");
        Assert.That(userDescriptor, Is.Not.Null);
        Assert.That(
            userDescriptor!.ImplementationType!.FullName,
            Does.Contain("PostgreSQL"),
            "Should register the PostgreSQL user repository implementation.");
    }

    [Test]
    public void AddRepositories_DefaultParameter_ShouldUseLegacyDatabase()
    {
        ServiceCollection services = new();

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
        ServiceCollection services = new();

        IServiceCollection result = services.AddRepositories();

        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddRepositories_ShouldRegisterAsScoped()
    {
        ServiceCollection services = new();

        services.AddRepositories(useLegacyDatabase: true);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IOAuthTokenRepository));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void AddRedis_WithRawServerIdentifier_ShouldRegisterIpcDependenciesAsSingletons()
    {
        ServiceCollection services = new();

        services.AddRedis("realtime");

        AssertRedisIpcDescriptors(services);
    }

    [Test]
    public void AddRedis_WithServerIdentify_ShouldRegisterIpcDependenciesAsSingletons()
    {
        ServiceCollection services = new();

        services.AddRedis(ServerIdentify.Realtime);

        AssertRedisIpcDescriptors(services);
    }

    [Test]
    public void AddCache_ShouldRegisterStringCacheAsSingleton()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddCache();

        ServiceDescriptor? cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IStringCache));

        Assert.That(result, Is.SameAs(services));
        Assert.That(cacheDescriptor, Is.Not.Null);
        Assert.That(cacheDescriptor!.ImplementationType, Is.EqualTo(typeof(RedisStringCache)));
        Assert.That(cacheDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddFetcher_ShouldRegisterFetcherServices()
    {
        ServiceCollection services = new();

        services.AddFetcher();

        ServiceDescriptor? httpDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHttpService));
        ServiceDescriptor? fetcherDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFetcher));

        Assert.That(httpDescriptor, Is.Not.Null);
        Assert.That(fetcherDescriptor, Is.Not.Null);
        Assert.That(fetcherDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void AddBackgroundTaskRunner_ShouldRegisterRunnerAsSingleton()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddBackgroundTaskRunner();

        ServiceDescriptor? runnerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(BackgroundTaskRunner));
        ServiceDescriptor? interfaceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IBackgroundTaskRunner));

        Assert.That(result, Is.SameAs(services));
        Assert.That(runnerDescriptor, Is.Not.Null);
        Assert.That(runnerDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(interfaceDescriptor, Is.Not.Null);
        Assert.That(interfaceDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddOAuthAuthentication_ShouldRegisterAuthorizationServices()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddOAuthAuthentication();

        ServiceDescriptor? policyProviderDescriptor = services.LastOrDefault(
            d => d.ServiceType == typeof(IAuthorizationPolicyProvider));
        ServiceDescriptor? handlerDescriptor = services.FirstOrDefault(
            d => d.ImplementationType == typeof(g0v0.Server.Common.Authentication.ScopeAuthorizationHandler));
        ServiceDescriptor? optionsDescriptor = services.FirstOrDefault(
            d => d.ImplementationType == typeof(g0v0.Server.Common.Authentication.ConfigureJwtBearerOptions));

        Assert.That(result, Is.SameAs(services));

        Assert.That(policyProviderDescriptor, Is.Not.Null);
        Assert.That(
            policyProviderDescriptor!.ImplementationType,
            Is.EqualTo(typeof(g0v0.Server.Common.Authentication.ScopePolicyProvider)));
        Assert.That(policyProviderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(handlerDescriptor, Is.Not.Null);
        Assert.That(
            handlerDescriptor!.ImplementationType,
            Is.EqualTo(typeof(g0v0.Server.Common.Authentication.ScopeAuthorizationHandler)));
        Assert.That(handlerDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(optionsDescriptor, Is.Not.Null);
    }

    private static void AssertRedisIpcDescriptors(ServiceCollection services)
    {
        ServiceDescriptor? redisDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
        ServiceDescriptor? transportDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IInterProcessCommunicationTransport));
        ServiceDescriptor? ipcClientDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(InterProcessCommunicationClient));

        Assert.That(redisDescriptor, Is.Not.Null);
        Assert.That(redisDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(transportDescriptor, Is.Not.Null);
        Assert.That(transportDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(ipcClientDescriptor, Is.Not.Null);
        Assert.That(ipcClientDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }
}