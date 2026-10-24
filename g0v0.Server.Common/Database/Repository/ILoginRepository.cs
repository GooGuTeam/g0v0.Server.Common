// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>Persists authentication evidence and login sessions.</summary>
public interface ILoginRepository
{
    /// <summary>Reads the password hash explicitly, without exposing it on the user model.</summary>
    /// <param name="userId">The account ID.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The stored bcrypt hash.</returns>
    Task<string?> GetPasswordHashAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Finds a device belonging to this account and hash.</summary>
    /// <param name="userId">The account ID.</param>
    /// <param name="clientType">The client type of the device.</param>
    /// <param name="deviceHash">The device hash.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The tracked device, if present.</returns>
    Task<TrustedDevice?> GetDeviceAsync(int userId, string clientType, string deviceHash, CancellationToken cancellationToken = default);

    /// <summary>Finds the latest unexpired session for this account and device.</summary>
    /// <param name="userId">The account ID.</param>
    /// <param name="clientType">The client type of the session.</param>
    /// <param name="deviceHash">The device hash.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The tracked session, if present.</returns>
    Task<LoginSession?> GetSessionAsync(int userId, string clientType, string deviceHash, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Atomically saves session and device changes with the login audit record.</summary>
    /// <param name="session">The session, or null for a failed login.</param>
    /// <param name="device">The stable device, or null for a failed login.</param>
    /// <param name="log">The login attempt.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>A task representing persistence.</returns>
    Task RecordAsync(LoginSession? session, TrustedDevice? device, LoginLogSession log, CancellationToken cancellationToken = default);
}