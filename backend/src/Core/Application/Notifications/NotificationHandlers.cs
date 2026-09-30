using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Notifications;

public sealed class GetNotificationsHandler(INotificationStore store)
    : IRequestHandler<GetNotificationsQuery, Result<NotificationsListResponse>>
{
    public Task<Result<NotificationsListResponse>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken) =>
        store.GetForUserAsync(request.Access, request.Page, request.PageSize, cancellationToken);
}

public sealed class MarkNotificationReadHandler(INotificationStore store)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public Task<Result> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken) =>
        store.MarkAsReadAsync(request.NotificationId, request.Access, cancellationToken);
}
