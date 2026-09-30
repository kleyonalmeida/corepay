using WebApp.Blazor.Components.Ui;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class NotificationToneMapper
{
    public static (SemanticTone Tone, IconKind Icon, string IconCssClass) Resolve(string? type) =>
        type switch
        {
            NotificationTypes.PayrollRejected => (
                SemanticTone.Red,
                IconKind.X,
                "notification-row__icon--red"),
            NotificationTypes.PayrollSubmitted => (
                SemanticTone.Yellow,
                IconKind.Bell,
                "notification-row__icon--yellow"),
            NotificationTypes.PayrollApproved => (
                SemanticTone.Emerald,
                IconKind.CheckCircle2,
                "notification-row__icon--emerald"),
            _ => (
                SemanticTone.Blue,
                IconKind.Bell,
                "notification-row__icon--blue")
        };
}
