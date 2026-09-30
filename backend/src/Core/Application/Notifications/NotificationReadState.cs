using Core.Domain;

namespace Core.Application.Notifications;

public static class NotificationReadState
{
    public static bool IsReadByUser(
        Notification notification,
        string userId,
        IReadOnlySet<Guid> readNotificationIds)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(readNotificationIds);

        return readNotificationIds.Contains(notification.Id);
    }
}
