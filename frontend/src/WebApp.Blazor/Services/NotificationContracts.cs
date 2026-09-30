namespace WebApp.Blazor.Services;

public static class NotificationTypes
{
    public const string PayrollSubmitted = "payroll_submitted";
    public const string PayrollApproved = "payroll_approved";
    public const string PayrollRejected = "payroll_rejected";
}

public enum NotificationApiStatus
{
    Success,
    NotFound,
    Error
}

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid PayrollId,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record NotificationsListDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int TotalCount = 0,
    int Page = 1,
    int PageSize = ListPagination.PageSize);

public sealed record NotificationsListResult(
    NotificationApiStatus Status,
    NotificationsListDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record NotificationMarkReadResult(
    NotificationApiStatus Status,
    string? ErrorCode = null,
    string? Message = null);
