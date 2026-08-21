// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for user relationship records.
/// </summary>
public interface IRelationshipRepository
{
    /// <summary>
    /// Gets the IDs of all users that the specified user is following.
    /// </summary>
    /// <param name="userId">The user ID to retrieve friend IDs for.</param>
    /// <returns>A array of friend IDs.</returns>
    Task<int[]> GetAllFriendIds(int userId);

    /// <summary>
    /// Gets all relationships where the specified user is the source (e.g., who they follow/block).
    /// </summary>
    /// <param name="userId">The source user ID.</param>
    /// <returns>A list of relationships initiated by the user.</returns>
    Task<IReadOnlyList<Relationship>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Gets all relationships where the specified user is the target (e.g., their followers).
    /// </summary>
    /// <param name="targetId">The target user ID.</param>
    /// <returns>A list of relationships targeting the user.</returns>
    Task<IReadOnlyList<Relationship>> GetByTargetIdAsync(int targetId);

    /// <summary>
    /// Gets a specific relationship between two users.
    /// </summary>
    /// <param name="userId">The source user ID.</param>
    /// <param name="targetId">The target user ID.</param>
    /// <returns>The matching relationship if found; otherwise, <see langword="null"/>.</returns>
    Task<Relationship?> GetRelationshipAsync(int userId, int targetId);

    /// <summary>
    /// Checks whether the source user is following the target user.
    /// </summary>
    /// <param name="userId">The source user ID.</param>
    /// <param name="targetId">The target user ID.</param>
    /// <returns><see langword="true"/> if a follow relationship exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsFollowingAsync(int userId, int targetId);

    /// <summary>
    /// Checks whether the source user has blocked the target user.
    /// </summary>
    /// <param name="userId">The source user ID.</param>
    /// <param name="targetId">The target user ID.</param>
    /// <returns><see langword="true"/> if a block relationship exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsBlockedAsync(int userId, int targetId);

    /// <summary>
    /// Creates a new relationship record.
    /// </summary>
    /// <param name="relationship">The relationship to create.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(Relationship relationship);

    /// <summary>
    /// Updates an existing relationship record.
    /// </summary>
    /// <param name="relationship">The relationship to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(Relationship relationship);

    /// <summary>
    /// Removes a relationship record.
    /// </summary>
    /// <param name="relationship">The relationship to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(Relationship relationship);
}