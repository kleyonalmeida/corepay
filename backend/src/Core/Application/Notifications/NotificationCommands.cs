using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Notifications;

public sealed record MarkNotificationReadCommand(
    Guid NotificationId,
    NotificationAccessContext Access)
    : IRequest<Result>;
