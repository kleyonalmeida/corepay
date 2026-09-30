namespace WebApp.Blazor.Services;

public sealed class NotificationState(INotificationApiService notificationApiService)
{
    private static readonly TimeSpan RefreshTtl = TimeSpan.FromSeconds(45);

    private bool _isRefreshing;
    private DateTimeOffset? _lastLoadedAt;
    private int _loadedPage = 1;

    public IReadOnlyList<NotificationDto> Items { get; private set; } = [];

    public int UnreadCount { get; private set; }

    public int TotalCount { get; private set; }

    public int Page { get; private set; } = 1;

    public int PageSize { get; private set; } = ListPagination.PageSize;

    public event Action? StateChanged;

    public async Task<NotificationsListResult> LoadAsync(
        int page = 1,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (_isRefreshing)
        {
            return new NotificationsListResult(NotificationApiStatus.Error);
        }

        if (!force
            && _lastLoadedAt is not null
            && DateTimeOffset.UtcNow - _lastLoadedAt.Value < RefreshTtl
            && _loadedPage == page)
        {
            return new NotificationsListResult(
                NotificationApiStatus.Success,
                new NotificationsListDto(Items, UnreadCount, TotalCount, Page, PageSize));
        }

        _isRefreshing = true;
        try
        {
            var result = await notificationApiService.GetAsync(
                page,
                ListPagination.PageSize,
                cancellationToken);
            if (result.Status == NotificationApiStatus.Success && result.Data is not null)
            {
                Items = result.Data.Items;
                UnreadCount = result.Data.UnreadCount;
                TotalCount = result.Data.TotalCount;
                Page = result.Data.Page;
                PageSize = result.Data.PageSize;
                _loadedPage = page;
                _lastLoadedAt = DateTimeOffset.UtcNow;
                Notify();
            }

            return result;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default) =>
        LoadAsync(1, force, cancellationToken);

    public async Task<bool> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var result = await notificationApiService.MarkReadAsync(notificationId, cancellationToken);
        if (result.Status != NotificationApiStatus.Success)
        {
            return false;
        }

        var index = Items.ToList().FindIndex(notification => notification.Id == notificationId);
        if (index >= 0)
        {
            var current = Items[index];
            if (!current.IsRead)
            {
                var updated = current with { IsRead = true };
                var list = Items.ToList();
                list[index] = updated;
                Items = list;
                UnreadCount = Math.Max(0, UnreadCount - 1);
                Notify();
            }
        }
        else
        {
            await RefreshAsync(force: true, cancellationToken);
        }

        return true;
    }

    private void Notify() => StateChanged?.Invoke();
}
