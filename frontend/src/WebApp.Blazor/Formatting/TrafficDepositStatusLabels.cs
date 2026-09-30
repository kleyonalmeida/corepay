using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class TrafficDepositStatusLabels
{
    public static string Format(TrafficDepositStatusDto status) =>
        status switch
        {
            TrafficDepositStatusDto.Pending => "Pendente",
            TrafficDepositStatusDto.Requested => "Solicitado",
            TrafficDepositStatusDto.Deposited => "Depositado",
            _ => status.ToString()
        };
}
