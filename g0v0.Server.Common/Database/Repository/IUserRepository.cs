// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for user records.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The primary key of the user.</param>
    /// <returns>The matching user if found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(int userId);

    /// <summary>
    /// Gets users by their unique identifiers.
    /// </summary>
    /// <param name="userIds">The user IDs to load.</param>
    /// <returns>The matching users.</returns>
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<int> userIds);

    /// <summary>
    /// Gets the username for a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The username if found; otherwise, <see langword="null"/>.</returns>
    Task<string?> GetUsernameByIdAsync(int userId);

    /// <summary>
    /// Gets a user by their unique username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The matching user if found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Checks whether a username is already taken.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns><see langword="true"/> if the username exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Gets a user by their unique email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The matching user if found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Creates a new user record.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(User user);

    /// <summary>
    /// Creates a new user record together with the initial per-mode statistics rows.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="statisticsModes">The mode IDs to seed statistics for.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Registration writes the account and its statistics as one unit of work, so a failure cannot
    /// leave an account without statistics that a retry would then report as an already taken
    /// username. <see cref="User.Id"/> is populated from the inserted row.
    /// </remarks>
    Task CreateAsync(User user, IReadOnlyList<int> statisticsModes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user record.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(User user);
}