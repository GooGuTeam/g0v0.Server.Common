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
/// <typeparam name="T">The configuration type.</typeparam>
public class ConfigurationManager<T>
{
    private const string ConfigBasePath = "config";

    private readonly T _value;
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationManager{T}"/> class and loads the configuration from the file.
    /// </summary>
    /// <param name="basePath">The base path to find `config/{filename}.json`.</param>
    public ConfigurationManager(string basePath)
    {
        _filePath = GetFilePath(basePath);

        _value = LoadConfiguration();
    }

    /// <summary>
    /// Gets the current loaded configuration value.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Reload the configuration from the file. Only properties marked with <see cref="ReloadableAttribute"/> will be updated.
    /// </summary>
    public void Reload()
    {
        T config = LoadConfiguration();
        var properties = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ReloadableAttribute>() != null);
        foreach (var property in properties)
        {
            var newValue = property.GetValue(config);
            property.SetValue(_value, newValue);
        }
    }

    private static string GetFilePath(string basePath)
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

    private T LoadConfiguration()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {_filePath}");
        }

        string jsonRaw = File.ReadAllText(_filePath);
        T? config = JsonConvert.DeserializeObject<T>(jsonRaw);
        return config ?? throw new InvalidDataException($"Failed to deserialize configuration file: {_filePath}");
    }
}