namespace Core.Application.Notifications;

public sealed record NotificationAccessContext(
    string UserId,
    IReadOnlyList<string> Roles);

public sealed record NotificationListItemResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid PayrollId,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record NotificationsListResponse(
    IReadOnlyList<NotificationListItemResponse> Items,
    int UnreadCount,
    int TotalCount,
    int Page,
    int PageSize);
