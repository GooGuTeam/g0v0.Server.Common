// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Communication;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.MySQL.Repository;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Http;
using g0v0.Server.Common.Storage;
using g0v0.Server.Common.Threading;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace g0v0.Server.Common.Extensions;

/// <summary>
/// Provides dependency injection registration helpers for Common project services.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers repository interfaces to matching implementations for the selected database backend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="useLegacyDatabase">
    /// <see langword="true"/> to register MySQL repositories; <see langword="false"/> to register PostgreSQL repositories.
    /// </param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services, bool useLegacyDatabase = true)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        Type[] types = assembly.GetTypes();

        IEnumerable<Type> interfaces = types
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        Type markerType = useLegacyDatabase ? typeof(IMySqlRepository) : typeof(IPostgreSqlRepository);

        foreach (Type? iface in interfaces)
        {
            Type? impl = types.FirstOrDefault(t =>
                t is { IsClass: true, IsAbstract: false } &&
                string.Equals(iface.Name[1..], t.Name, StringComparison.Ordinal) &&
                markerType.IsAssignableFrom(t));

            if (impl != null)
            {
                services.AddScoped(iface, impl);
            }
        }

        return services;
    }

    /// <summary>
    /// Registers the Redis connection multiplexer, IPC transport, and inter-process communication client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serverIdentify">The server identifier used for IPC channel naming.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRedis(this IServiceCollection services, string serverIdentify)
    {
        services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(serviceProvider =>
        {
            ConfigurationManager manager = serviceProvider.GetRequiredService<ConfigurationManager>();
            GeneralConfiguration generalConfig = manager.Get<GeneralConfiguration>();
            return ConnectionMultiplexer.Connect(generalConfig.RedisHost);
        });
        services.AddSingleton<IInterProcessCommunicationTransport>(serviceProvider =>
        {
            IConnectionMultiplexer connection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            return new RedisInterProcessCommunicationTransport(connection);
        });
        services.AddSingleton<InterProcessCommunicationClient>(serviceProvider =>
        {
            IInterProcessCommunicationTransport transport = serviceProvider.GetRequiredService<IInterProcessCommunicationTransport>();
            return new InterProcessCommunicationClient(transport, serverIdentify);
        });
        return services;
    }

    /// <summary>
    /// Registers the configured storage service implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Requires <see cref="ConfigurationManager"/> to be registered beforehand. Local storage also requires
    /// <see cref="IPathProvider"/> so relative paths can be resolved from the server base path.
    /// </remarks>
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddSingleton<StorageService>(serviceProvider =>
        {
            ConfigurationManager manager = serviceProvider.GetRequiredService<ConfigurationManager>();
            StorageConfiguration storageConfig = manager.Get<StorageConfiguration>();
            IPathProvider? pathProvider = storageConfig.Type == StorageConfiguration.StorageType.Local
                ? serviceProvider.GetRequiredService<IPathProvider>()
                : null;

            return StorageServiceFactory.Create(storageConfig, pathProvider);
        });
        services.AddSingleton<IStorageService>(serviceProvider => serviceProvider.GetRequiredService<StorageService>());

        return services;
    }

    /// <summary>
    /// Registers the shared background task runner.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBackgroundTaskRunner(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundTaskRunner>();
        services.AddSingleton<IBackgroundTaskRunner>(serviceProvider => serviceProvider.GetRequiredService<BackgroundTaskRunner>());

        return services;
    }

    /// <summary>
    /// Registers the shared string cache used across g0v0 server services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Requires <c>AddRedis</c> to be registered beforehand so an <see cref="IConnectionMultiplexer"/> is available.
    /// </remarks>
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddSingleton<IStringCache, RedisStringCache>();

        return services;
    }

    /// <summary>
    /// Registers the osu! Fetcher service and its shared HTTP dependency.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Requires <see cref="ConfigurationManager"/>, <c>AddRepositories</c>, <c>AddRedis</c> and <c>AddCache</c> to be registered beforehand.
    /// </remarks>
    public static IServiceCollection AddFetcher(this IServiceCollection services)
    {
        services.AddHttpClient<IHttpService, HttpService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<Fetcher>();
        services.AddScoped<IFetcher>(serviceProvider => serviceProvider.GetRequiredService<Fetcher>());

        return services;
    }

    /// <summary>
    /// Registers the shared g0v0 OAuth/JWT authentication and authorization components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Requires <see cref="ConfigurationManager"/> and
    /// <c>IOAuthTokenRepository</c> to be registered beforehand.
    /// </remarks>
    public static IServiceCollection AddOAuthAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, ScopePolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        return services;
    }
}