// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Configures login verification, multiple-device sessions, and device trust.
/// </summary>
[ConfigFile("verification")]
public class VerificationConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether TOTP verification is enabled.
    /// </summary>
    public bool EnableTotpVerification { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional TOTP issuer; the service name is used when absent.
    /// </summary>
    public string? TotpIssuer { get; set; }

    /// <summary>
    /// Gets or sets the service name used in TOTP labels.
    /// </summary>
    public string TotpServiceName { get; set; } = "g0v0! Lazer Server";

    /// <summary>
    /// Gets or sets a value indicating whether TOTP labels use usernames instead of email addresses.
    /// </summary>
    public bool TotpUseUsernameInLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Turnstile verification is enabled.
    /// </summary>
    public bool EnableTurnstileVerification { get; set; }

    /// <summary>
    /// Gets or sets the Turnstile secret key.
    /// </summary>
    public string TurnstileSecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether Turnstile development mode is enabled.
    /// </summary>
    public bool TurnstileDevMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether email verification is enabled.
    /// </summary>
    public bool EnableEmailVerification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether login session verification is enabled.
    /// </summary>
    public bool EnableSessionVerification { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether simultaneous device logins are allowed.
    /// </summary>
    public bool EnableMultiDeviceLogin { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of OAuth tokens per client.
    /// </summary>
    public int MaxTokensPerClient { get; set; } = 10;

    /// <summary>
    /// Gets or sets the device trust lifetime in days.
    /// </summary>
    public int DeviceTrustDurationDays { get; set; } = 30;
}