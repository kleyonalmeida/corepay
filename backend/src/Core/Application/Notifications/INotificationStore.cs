using BuildingBlocks.Results;
using Core.Domain;

namespace Core.Application.Notifications;

public interface INotificationStore
{
    void StagePayrollSubmitted(Payroll payroll, DateTimeOffset createdAt);

    void StagePayrollApproved(Payroll payroll, DateTimeOffset createdAt);

    void StagePayrollRejected(Payroll payroll, DateTimeOffset createdAt);

    Task<Result<NotificationsListResponse>> GetForUserAsync(
        NotificationAccessContext access,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<Result> MarkAsReadAsync(
        Guid notificationId,
        NotificationAccessContext access,
        CancellationToken cancellationToken = default);
}
