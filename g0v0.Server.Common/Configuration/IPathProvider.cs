// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// A provider for file paths.
/// </summary>
public interface IPathProvider
{
    /// <summary>
    /// Gets the base path to find files.
    /// The configuration file is expected to be located in the "config" directory under this base path.
    /// The ruleset assembly is expected to be located in the "rulesets" directory under this base path.
    /// </summary>
    /// <returns>The base path of files.</returns>
    string GetBasePath();
}