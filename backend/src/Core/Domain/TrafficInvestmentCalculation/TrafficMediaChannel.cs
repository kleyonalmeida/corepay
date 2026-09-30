namespace Core.Domain.TrafficInvestmentCalculation;

/// <summary>
/// Canais de mídia para gasto de tráfego (REGRAS §8.1).
/// Distinto de <see cref="ProjectPlatform"/> (Lastlink/Hubla).
/// </summary>
public enum TrafficMediaChannel
{
    Telegram,
    Instagram,
    Story,
    Direct,
    Remarketing,
    Other
}
