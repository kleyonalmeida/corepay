using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Notifications;

public sealed record GetNotificationsQuery(
    NotificationAccessContext Access,
    int? Page = null,
    int? PageSize = null)
    : IRequest<Result<NotificationsListResponse>>;
