// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json.Serialization;

namespace g0v0.Server.Common.Json;

/// <summary>
/// A <see cref="NamingStrategy"/> that converts property names to lowercase.
/// </summary>
internal sealed class LowercaseNamingStrategy : NamingStrategy
{
    /// <inheritdoc />
    protected override string ResolvePropertyName(string name)
        => name.ToLowerInvariant();
}