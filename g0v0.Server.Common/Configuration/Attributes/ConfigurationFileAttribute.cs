// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Configuration.Attributes;

/// <summary>
/// Specifies the filename for a configuration class. The extension of file is not required. The extension is `.json`.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationFileAttribute(string fileName) : Attribute
{
    /// <summary>
    /// Gets the filename of configuration file.
    /// </summary>
    public string FileName { get; } = fileName;
}