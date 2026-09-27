// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Extensions;

/// <summary>
/// Provides helpers for strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Add a suffix to a string if suffix don't exist.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <param name="suffix">The suffix.</param>
    /// <returns>The string with specified suffix.</returns>
    public static string AddSuffix(this string value, string suffix)
    {
        if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }
        return value + suffix;
    }
}