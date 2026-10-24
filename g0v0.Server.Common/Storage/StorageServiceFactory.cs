// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using Newtonsoft.Json.Linq;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Creates storage service implementations from storage configuration.
/// </summary>
public static class StorageServiceFactory
{
    /// <summary>
    /// Creates a storage service for the supplied configuration.
    /// </summary>
    /// <param name="configuration">The storage configuration.</param>
    /// <param name="pathProvider">The path provider required by local storage.</param>
    /// <returns>The created storage service.</returns>
    public static StorageService Create(StorageConfiguration configuration, IPathProvider? pathProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.Type switch
        {
            StorageConfiguration.StorageType.Local => new LocalStorageService(
                ReadOptions<LocalStorageSettings>(configuration),
                pathProvider ?? throw new InvalidOperationException("Local storage requires an IPathProvider.")),
            StorageConfiguration.StorageType.S3 => new S3StorageService(ReadOptions<S3StorageSettings>(configuration)),
            StorageConfiguration.StorageType.R2 => new CloudflareR2StorageService(ReadOptions<CloudflareR2Settings>(configuration)),
            _ => throw new NotSupportedException($"Unsupported storage service: {configuration.Type}"),
        };
    }

    private static T ReadOptions<T>(StorageConfiguration configuration)
        where T : new()
    {
        return configuration.Options.Type == JTokenType.Null
            ? new T()
            : configuration.Options.ToObject<T>() ??
               throw new InvalidDataException($"Failed to parse storage options as {typeof(T).Name}.");
    }
}