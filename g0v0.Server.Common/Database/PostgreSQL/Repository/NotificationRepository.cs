// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// Persists notifications in the v2 PostgreSQL schema.
/// </summary>
/// <param name="context">The PostgreSQL database context.</param>
public class NotificationRepository(PostgreSqlDbContext context) : INotificationRepository, IPostgreSqlRepository
{
    /// <inheritdoc />
    public async Task<Notification> CreateAsync(Notification notification, IReadOnlyList<int> receiverIds)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync().ConfigureAwait(false)
            : null;

        try
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync().ConfigureAwait(false);

            context.UserNotifications.AddRange(receiverIds
                .Distinct()
                .Select(receiverId => new UserNotification
                {
                    NotificationId = notification.Id,
                    UserId = receiverId,
                    IsRead = false,
                }));
            await context.SaveChangesAsync().ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            return notification;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly, long? maxId, int limit)
    {
        IQueryable<UserNotification> query = context.UserNotifications
            .Include(userNotification => userNotification.Notification)
            .Where(userNotification => userNotification.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(userNotification => !userNotification.IsRead);
        }

        if (maxId.HasValue)
        {
            query = query.Where(userNotification => userNotification.NotificationId < maxId.Value);
        }

        return await query
            .OrderByDescending(userNotification => userNotification.NotificationId)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> CountUnreadAsync(int userId)
    {
        return context.UserNotifications.CountAsync(
            userNotification => userNotification.UserId == userId && !userNotification.IsRead);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetNotificationIdsAsync(IReadOnlyList<string> names, string? objectType, long? objectId, int? id)
    {
        IQueryable<Notification> query = context.Notifications.AsQueryable();

        if (names.Count > 0)
        {
            query = query.Where(notification => names.Contains(notification.Name));
        }

        if (objectType != null)
        {
            query = query.Where(notification => notification.ObjectType == objectType);
        }

        if (objectId.HasValue)
        {
            query = query.Where(notification => notification.ObjectId == objectId.Value);
        }

        if (id.HasValue)
        {
            query = query.Where(notification => notification.Id == id.Value);
        }

        return await query
            .Select(notification => notification.Id)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> MarkAsReadAsync(int userId, IReadOnlyList<int> notificationIds)
    {
        List<UserNotification> records = await context.UserNotifications
            .Where(userNotification => userNotification.UserId == userId
                && !userNotification.IsRead
                && notificationIds.Contains(userNotification.NotificationId))
            .ToListAsync();

        foreach (UserNotification record in records)
        {
            record.IsRead = true;
        }

        await context.SaveChangesAsync();
        return records.Count;
    }
}