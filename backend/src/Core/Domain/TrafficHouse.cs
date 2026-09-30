namespace Core.Domain;

/// <summary>
/// Casas de apostas para CPA de tráfego pago (paridade com TRAFFIC_HOUSES do legado).
/// </summary>
public static class TrafficHouse
{
    public const string Esportiva = "esportiva";
    public const string Stake = "stake";
    public const string Betano = "betano";
    public const string BetMgm = "betmgm";
    public const string Novibet = "novibet";
    public const string BetFair = "betfair";
    public const string Blaze = "blaze";
    public const string Superbet = "superbet";
    public const string Hiperbet = "hiperbet";

    public static readonly IReadOnlyList<string> All =
    [
        Esportiva,
        Stake,
        Betano,
        BetMgm,
        Novibet,
        BetFair,
        Blaze,
        Superbet,
        Hiperbet
    ];
}
