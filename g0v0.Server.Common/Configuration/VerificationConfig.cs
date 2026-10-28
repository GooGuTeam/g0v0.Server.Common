// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Configures login verification, multiple-device sessions, device trust, and client IP resolution.
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

    #region Client IP

    /// <summary>
    /// Gets or sets the CIDR ranges (or bare addresses) whose connections may supply
    /// <c>X-Forwarded-For</c>, <c>X-Real-IP</c> and <c>CF-Connecting-IP</c>.
    /// </summary>
    /// <remarks>
    /// The defaults cover loopback and private networks, which is what a reverse proxy running on
    /// the same host or in the same container network looks like. They do not cover public CDN
    /// edges; publish those through <see cref="TrustedProxySources"/> instead of hard-coding them.
    /// Only list proxies here: a trusted peer can rewrite its own identity.
    /// </remarks>
    public string[] TrustedProxies { get; set; } =
    [
        "127.0.0.0/8",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "fc00::/7",
    ];

    /// <summary>
    /// Gets or sets the URLs that publish the edge ranges of the CDN in front of the proxy.
    /// </summary>
    /// <remarks>
    /// Plain text, JSON and CSV work because every address or CIDR token in the payload is used;
    /// bare addresses become single-host ranges. Fetched ranges are merged into
    /// <see cref="TrustedProxies"/> and refreshed on <see cref="TrustedProxyRefreshMinutes"/>. A
    /// failed refresh keeps the previous snapshot, and catch-all ranges are ignored.
    /// </remarks>
    public string[] TrustedProxySources { get; set; } = [];

    /// <summary>
    /// Gets or sets how often <see cref="TrustedProxySources"/> are reloaded, in minutes.
    /// </summary>
    /// <remarks>Values below one minute are treated as one minute.</remarks>
    public int TrustedProxyRefreshMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the timeout for one <see cref="TrustedProxySources"/> request, in seconds.
    /// </summary>
    /// <remarks>Values below one second are treated as one second.</remarks>
    public int TrustedProxySourceTimeoutSeconds { get; set; } = 5;

    #endregion
}