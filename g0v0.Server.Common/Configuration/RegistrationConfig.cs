// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Configures the in-game account registration screen of the stable client.
/// </summary>
[ConfigFile("registration")]
public class RegistrationConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the stable client may create an account in game.
    /// </summary>
    public bool AllowInGameRegistration { get; set; } = true;

    /// <summary>
    /// Gets or sets the usernames that may not be registered, compared case-insensitively.
    /// </summary>
    public string[] DisallowedUsernames { get; set; } =
    [
        "mrekk",
        "vaxei",
        "btmc",
        "cookiezi",
        "peppy",
        "saragi",
        "chocomint",
    ];
}