namespace WebApp.Blazor.Formatting;

public static class TrafficHouseLabels
{
    public static readonly IReadOnlyList<TrafficHouseField> All =
    [
        new("esportiva", "Esportiva", "trafficCpaEsportiva"),
        new("stake", "Stake", "trafficCpaStake"),
        new("betano", "Betano", "trafficCpaBetano"),
        new("betmgm", "BetMGM", "trafficCpaBetMgm"),
        new("novibet", "Novibet", "trafficCpaNovibet"),
        new("betfair", "BetFair", "trafficCpaBetFair"),
        new("blaze", "Blaze", "trafficCpaBlaze"),
        new("superbet", "Superbet", "trafficCpaSuperbet"),
        new("hiperbet", "Hiperbet", "trafficCpaHiperbet")
    ];
}

public sealed record TrafficHouseField(string Key, string Label, string PropertyName);
