// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// A provider for configuration file paths.
/// </summary>
public interface IConfigPathProvider
{
    /// <summary>
    /// Gets the base path to find configuration files. The configuration file is expected to be located in the "config" directory under this base path.
    /// </summary>
    /// <returns>The base path of configuration files.</returns>
    string GetBasePath();
}