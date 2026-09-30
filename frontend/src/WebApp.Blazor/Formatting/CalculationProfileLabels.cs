using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class CalculationProfileLabels
{
    private static readonly IReadOnlyDictionary<CalculationProfile, string> Labels =
        new Dictionary<CalculationProfile, string>
        {
            [CalculationProfile.CommercialAnalyst] = "Analista comercial",
            [CalculationProfile.CommercialSupervisor] = "Supervisor comercial",
            [CalculationProfile.PaidTraffic] = "Tráfego pago",
            [CalculationProfile.Management] = "Gerência",
            [CalculationProfile.ProjectLeader] = "Líder de projetos",
            [CalculationProfile.CommissionOnly] = "Somente comissão",
            [CalculationProfile.FixedCommission] = "Fixo + comissão",
            [CalculationProfile.FixedCommissionBonus] = "Fixo + comissão + bônus",
            [CalculationProfile.FixedBonus] = "Fixo + bônus",
            [CalculationProfile.Tipster] = "Tipster",
            [CalculationProfile.AllocatedFixed] = "Fixo rateado"
        };

    public static IReadOnlyList<CalculationProfile> AllProfiles { get; } =
        Enum.GetValues<CalculationProfile>().ToArray();

    public static string GetLabel(CalculationProfile profile) =>
        Labels.TryGetValue(profile, out var label) ? label : profile.ToString();
}
