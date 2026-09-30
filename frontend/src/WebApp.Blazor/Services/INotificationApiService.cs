namespace WebApp.Blazor.Services;

public interface INotificationApiService
{
    Task<NotificationsListResult> GetAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<NotificationMarkReadResult> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
