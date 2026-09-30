using Core.Domain;

namespace Core.Application.Notifications;

public static class NotificationVisibility
{
    public static bool IsVisibleTo(Notification notification, NotificationAccessContext access)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(access);

        if (!string.IsNullOrWhiteSpace(notification.UserId)
            && string.Equals(notification.UserId, access.UserId, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(notification.RoleTarget))
        {
            return access.Roles.Contains(
                notification.RoleTarget,
                StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }
}
