using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class DashboardErrorMessages
{
    public static string ForLoad(DashboardResult result) =>
        result.Status switch
        {
            DashboardApiStatus.ValidationError when result.ErrorCode == "dashboard.invalid_month"
                => "Selecione um mês válido.",
            DashboardApiStatus.ValidationError when result.ErrorCode == "dashboard.invalid_year"
                => "Selecione um ano válido.",
            DashboardApiStatus.ValidationError
                => result.Message ?? "Verifique a competência informada.",
            _ => "Não foi possível carregar o dashboard."
        };
}
