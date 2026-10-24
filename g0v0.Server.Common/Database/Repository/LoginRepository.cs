// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>Shares login persistence between the database providers.</summary>
/// <typeparam name="TContext">The provider context.</typeparam>
/// <param name="context">The request-scoped context.</param>
public abstract class LoginRepository<TContext>(TContext context) : ILoginRepository
    where TContext : DbContext
{
    /// <inheritdoc/>
    public Task<string?> GetPasswordHashAsync(int userId, CancellationToken cancellationToken = default)
        => context.Set<User>().Where(user => user.Id == userId)
            .Select(user => user.PwBcrypt).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<TrustedDevice?> GetDeviceAsync(int userId, string clientType, string deviceHash, CancellationToken cancellationToken = default)
        => context.Set<TrustedDevice>().FirstOrDefaultAsync(
            device => device.UserId == userId && device.ClientType == clientType && device.DeviceHash == deviceHash,
            cancellationToken);

    /// <inheritdoc/>
    public Task<LoginSession?> GetSessionAsync(int userId, string clientType, string deviceHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        => context.Set<LoginSession>().Where(session => session.UserId == userId && session.DeviceHash == deviceHash
            && session.Device != null && session.Device.ClientType == clientType && session.ExpiresAt > now)
            .OrderByDescending(session => session.Id).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task RecordAsync(LoginSession? session, TrustedDevice? device, LoginLogSession log, CancellationToken cancellationToken = default)
    {
        if (device != null && context.Entry(device).State == EntityState.Detached)
        {
            context.Add(device);
        }

        if (session != null && context.Entry(session).State == EntityState.Detached)
        {
            context.Add(session);
        }

        context.Add(log);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}