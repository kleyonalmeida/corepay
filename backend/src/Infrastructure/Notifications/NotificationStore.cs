using BuildingBlocks.Pagination;
using BuildingBlocks.Results;
using Core.Application.Notifications;
using Core.Auth;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

public sealed class NotificationStore(AppDbContext dbContext) : INotificationStore
{
    public void StagePayrollSubmitted(Payroll payroll, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        var (title, message) = PayrollNotificationContentBuilder.BuildSubmitted(payroll);
        StageRoleTargetNotification(
            NotificationTypes.PayrollSubmitted,
            title,
            message,
            payroll.Id,
            AppRoles.Director,
            createdAt);
        StageRoleTargetNotification(
            NotificationTypes.PayrollSubmitted,
            title,
            message,
            payroll.Id,
            AppRoles.Admin,
            createdAt);
    }

    public void StagePayrollApproved(Payroll payroll, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        if (string.IsNullOrWhiteSpace(payroll.SubmittedByUserId))
        {
            return;
        }

        var (title, message) = PayrollNotificationContentBuilder.BuildApproved(payroll);
        StageUserNotification(
            NotificationTypes.PayrollApproved,
            title,
            message,
            payroll.Id,
            payroll.SubmittedByUserId,
            createdAt);
    }

    public void StagePayrollRejected(Payroll payroll, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        if (string.IsNullOrWhiteSpace(payroll.SubmittedByUserId))
        {
            return;
        }

        var (title, message) = PayrollNotificationContentBuilder.BuildRejected(payroll);
        StageUserNotification(
            NotificationTypes.PayrollRejected,
            title,
            message,
            payroll.Id,
            payroll.SubmittedByUserId,
            createdAt);
    }

    public async Task<Result<NotificationsListResponse>> GetForUserAsync(
        NotificationAccessContext access,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize, skip) = Pagination.Normalize(page, pageSize);
        var visibleQuery = ApplyVisibilityFilter(
            dbContext.Notifications.AsNoTracking(),
            access);

        var totalCount = await visibleQuery.CountAsync(cancellationToken);

        var readNotificationIds = await dbContext.NotificationReadReceipts
            .AsNoTracking()
            .Where(receipt => receipt.UserId == access.UserId)
            .Select(receipt => receipt.NotificationId)
            .ToHashSetAsync(cancellationToken);

        var notifications = await visibleQuery
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip(skip)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var visible = notifications
            .Select(notification => MapToResponse(notification, access.UserId, readNotificationIds))
            .ToList();

        var unreadCount = await CountUnreadAsync(access, readNotificationIds, cancellationToken);

        return Result<NotificationsListResponse>.Success(
            new NotificationsListResponse(
                visible,
                unreadCount,
                totalCount,
                normalizedPage,
                normalizedPageSize));
    }

    public async Task<Result> MarkAsReadAsync(
        Guid notificationId,
        NotificationAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var notification = await ApplyVisibilityFilter(
                dbContext.Notifications.AsNoTracking(),
                access)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(
                Error.NotFound("notifications.not_found", "Notificação não encontrada."));
        }

        var alreadyRead = await dbContext.NotificationReadReceipts
            .AnyAsync(
                receipt => receipt.NotificationId == notificationId && receipt.UserId == access.UserId,
                cancellationToken);

        if (alreadyRead)
        {
            return Result.Success();
        }

        dbContext.NotificationReadReceipts.Add(new NotificationReadReceipt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            UserId = access.UserId,
            ReadAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static IQueryable<Notification> ApplyVisibilityFilter(
        IQueryable<Notification> query,
        NotificationAccessContext access)
    {
        var userId = access.UserId;
        var roles = access.Roles;

        return query.Where(notification =>
            (notification.UserId != null && notification.UserId == userId)
            || (notification.RoleTarget != null && roles.Contains(notification.RoleTarget)));
    }

    private async Task<int> CountUnreadAsync(
        NotificationAccessContext access,
        IReadOnlySet<Guid> readNotificationIds,
        CancellationToken cancellationToken)
    {
        var visibleIds = await ApplyVisibilityFilter(
                dbContext.Notifications.AsNoTracking(),
                access)
            .Select(notification => notification.Id)
            .ToListAsync(cancellationToken);

        return visibleIds.Count(id => !readNotificationIds.Contains(id));
    }

    private void StageRoleTargetNotification(
        string type,
        string title,
        string message,
        Guid payrollId,
        string roleTarget,
        DateTimeOffset createdAt)
    {
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Message = message,
            PayrollId = payrollId,
            RoleTarget = roleTarget,
            IsRead = false,
            CreatedAt = createdAt
        });
    }

    private void StageUserNotification(
        string type,
        string title,
        string message,
        Guid payrollId,
        string userId,
        DateTimeOffset createdAt)
    {
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Message = message,
            PayrollId = payrollId,
            UserId = userId,
            IsRead = false,
            CreatedAt = createdAt
        });
    }

    private static NotificationListItemResponse MapToResponse(
        Notification notification,
        string userId,
        IReadOnlySet<Guid> readNotificationIds) =>
        new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.PayrollId,
            NotificationReadState.IsReadByUser(notification, userId, readNotificationIds),
            notification.CreatedAt);
}
