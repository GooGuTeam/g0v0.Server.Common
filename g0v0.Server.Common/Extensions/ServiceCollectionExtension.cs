// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using g0v0.Server.Common.Database.MySQL.Repository;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.Extensions.DependencyInjection;

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
}