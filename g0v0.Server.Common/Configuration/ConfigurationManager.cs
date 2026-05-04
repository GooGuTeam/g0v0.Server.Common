// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using g0v0.Server.Common.Configuration.Attributes;
using Newtonsoft.Json;
using osu.Game.Extensions;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// A manager for loading and reloading configuration files.
/// The configuration file is expected to be in JSON format and located in the "config" directory under the specified base path.
/// The filename is determined by the type name of the configuration class, converted to snake_case,
/// or by a custom filename specified using the <see cref="ConfigurationFileAttribute"/>.
/// Only properties marked with the <see cref="ReloadableAttribute"/> will be updated when reloading the configuration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigurationManager"/> class and loads the configuration from the file.
/// </remarks>
/// <param name="basePath">The base path to find `config/{filename}.json`.</param>
public class ConfigurationManager(string basePath)
{
    private const string ConfigBasePath = "config";
    private readonly Dictionary<Type, object> _configCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationManager"/> class using a configuration path provider and loads the configuration from the file.
    /// </summary>
    /// <param name="pathProvider">The path provider.</param>
    public ConfigurationManager(IPathProvider pathProvider)
        : this(pathProvider.GetBasePath())
    {
    }

    /// <summary>
    /// Gets a configuration of the specified type. If the configuration is not yet cached, it will be loaded from the file.
    /// </summary>
    /// <typeparam name="T">The type of configuration to retrieve.</typeparam>
    /// <returns>The configuration instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configuration file cannot be loaded.</exception>
    public T Get<T>()
    {
        // try to get cached value
        if (_configCache.TryGetValue(typeof(T), out var cachedValue) && cachedValue is T cachedConfig)
        {
            return cachedConfig;
        }

        // otherwise, load from file and cache it
        var value = LoadConfiguration<T>() ??
                    throw new InvalidOperationException("Configuration of type " + typeof(T).FullName +
                                                        " is not loaded.");
        _configCache.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    /// Reload the configuration from the file. Only properties marked with <see cref="ReloadableAttribute"/> will be updated.
    /// </summary>
    /// <typeparam name="T">The type of configuration to reload.</typeparam>
    public void Reload<T>()
    {
        T config = LoadConfiguration<T>();
        var value = Get<T>();
        var properties = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ReloadableAttribute>() != null);
        foreach (var property in properties)
        {
            var newValue = property.GetValue(config);
            property.SetValue(value, newValue);
        }
    }

    private static string GetFilePath<T>(string basePath)
    {
        string filename;
        Type t = typeof(T);

        var attribute = t.GetCustomAttribute<ConfigurationFileAttribute>();
        filename = attribute != null ? attribute.FileName : t.Name.ToSnakeCase() + ".json";

        if (!filename.EndsWith(".json", StringComparison.Ordinal))
        {
            filename += ".json";
        }

        return Path.Combine(basePath, ConfigBasePath, filename);
    }

    private T LoadConfiguration<T>()
    {
        var filePath = GetFilePath<T>(basePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        string jsonRaw = File.ReadAllText(filePath);
        T? config = JsonConvert.DeserializeObject<T>(jsonRaw);
        return config ?? throw new InvalidDataException($"Failed to deserialize configuration file: {filePath}");
    }
}