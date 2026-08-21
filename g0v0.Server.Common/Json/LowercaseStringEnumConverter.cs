// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json.Converters;

namespace g0v0.Server.Common.Json;

/// <summary>
/// Custom <see cref="StringEnumConverter"/> that serializes enum values as lowercase.
/// </summary>
internal sealed class LowercaseStringEnumConverter : StringEnumConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LowercaseStringEnumConverter"/> class.
    /// </summary>
    public LowercaseStringEnumConverter()
        : base(new LowercaseNamingStrategy(), allowIntegerValues: false)
    {
    }
}