using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class NotificationErrorMessages
{
    public static string ForLoad(NotificationsListResult result) =>
        result.Status switch
        {
            NotificationApiStatus.NotFound => "Notificação não encontrada.",
            _ => "Não foi possível carregar as notificações."
        };

    public static string ForMarkRead(NotificationMarkReadResult result) =>
        result.Status switch
        {
            NotificationApiStatus.NotFound => "Notificação não encontrada.",
            _ => "Não foi possível marcar a notificação como lida."
        };
}
