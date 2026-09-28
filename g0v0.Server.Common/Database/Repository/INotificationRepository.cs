// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Persists notifications and their per-user delivery records, mirroring the
/// notification database helpers of the reference implementations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Creates a notification and one unread delivery record per receiver.
    /// </summary>
    /// <param name="notification">The notification to store.</param>
    /// <param name="receiverIds">The IDs of the users receiving the notification.</param>
    /// <returns>The stored notification with its database ID assigned.</returns>
    Task<Notification> CreateAsync(Notification notification, IReadOnlyList<int> receiverIds);

    /// <summary>
    /// Lists the notification delivery records of a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="unreadOnly">Whether only unread records are returned.</param>
    /// <param name="maxId">When set, only records of notifications with a smaller ID are returned.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    /// <returns>The delivery records ordered by descending notification ID.</returns>
    Task<IReadOnlyList<UserNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly, long? maxId, int limit);

    /// <summary>
    /// Counts the unread delivery records of a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The number of unread delivery records.</returns>
    Task<int> CountUnreadAsync(int userId);

    /// <summary>
    /// Resolves notification IDs by identity filters, mirroring osu-web's
    /// <c>Notification::byIdentity</c>: every filter is optional and only
    /// non-empty filters constrain the query.
    /// </summary>
    /// <param name="names">The notification names to match, empty for no name filter.</param>
    /// <param name="objectType">The object type to match, <see langword="null"/> for no filter.</param>
    /// <param name="objectId">The object ID to match, <see langword="null"/> for no filter.</param>
    /// <param name="id">The notification ID to match, <see langword="null"/> for no filter.</param>
    /// <returns>The matching notification IDs.</returns>
    Task<IReadOnlyList<int>> GetNotificationIdsAsync(IReadOnlyList<string> names, string? objectType, long? objectId, int? id);

    /// <summary>
    /// Marks a user's unread delivery records of the given notifications read.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="notificationIds">The notification IDs to mark read.</param>
    /// <returns>The number of records that were marked read.</returns>
    Task<int> MarkAsReadAsync(int userId, IReadOnlyList<int> notificationIds);
}