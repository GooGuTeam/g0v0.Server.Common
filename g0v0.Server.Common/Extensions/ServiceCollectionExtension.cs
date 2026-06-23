// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Communication;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.MySQL.Repository;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
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
        var assembly = Assembly.GetExecutingAssembly();

        var types = assembly.GetTypes();

        var interfaces = types
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        var markerType = useLegacyDatabase ? typeof(IMySqlRepository) : typeof(IPostgreSqlRepository);

        foreach (var iface in interfaces)
        {
            var impl = types.FirstOrDefault(t =>
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

    public static IServiceCollection AddRedis(this IServiceCollection services, string serverIdentify)
    {
        services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(serviceProvider =>
        {
            var manager = serviceProvider.GetRequiredService<ConfigurationManager>();
            var generalConfig = manager.Get<GeneralConfiguration>();
            return ConnectionMultiplexer.Connect(generalConfig.RedisHost);
        });
        services.AddSingleton<IInterProcessCommunicationTransport>(serviceProvider =>
        {
            var connection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            return new RedisInterProcessCommunicationTransport(connection);
        });
        services.AddSingleton<InterProcessCommunicationClient>(serviceProvider =>
        {
            var transport = serviceProvider.GetRequiredService<IInterProcessCommunicationTransport>();
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
            var manager = serviceProvider.GetRequiredService<ConfigurationManager>();
            var storageConfig = manager.Get<StorageConfiguration>();
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