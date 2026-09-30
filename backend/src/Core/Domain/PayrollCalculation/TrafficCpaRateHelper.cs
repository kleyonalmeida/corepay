using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Taxas R$/CPA por casa de tráfego pago (paridade TRAFFIC_HOUSES + §6.3).
/// </summary>
public static class TrafficCpaRateHelper
{
    public static Result<decimal> GetTrafficCpaRate(
        string houseKey,
        TrafficCpaKind kind,
        CareerLevel currentLevel,
        CareerLevel? seniorLevel)
    {
        ArgumentNullException.ThrowIfNull(houseKey);
        ArgumentNullException.ThrowIfNull(currentLevel);

        if (!TrafficHouse.All.Contains(houseKey))
        {
            return Result<decimal>.Failure(
                Error.Validation("traffic.invalid_house", $"Casa de tráfego inválida: '{houseKey}'."));
        }

        if (!Enum.IsDefined(kind))
        {
            return Result<decimal>.Failure(
                Error.Validation("traffic.invalid_cpa_kind", $"Tipo de CPA inválido: '{kind}'."));
        }

        var level = kind switch
        {
            TrafficCpaKind.Supervised => currentLevel,
            TrafficCpaKind.Manager => seniorLevel,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        if (level is null)
        {
            return Result<decimal>.Failure(
                Error.NotFound(
                    "traffic.senior_level_not_found",
                    "Nível Sênior de tráfego não encontrado para CPA tipo manager."));
        }

        return Result<decimal>.Success(ReadRateForHouse(level, houseKey));
    }

    private static decimal ReadRateForHouse(CareerLevel level, string houseKey) =>
        houseKey switch
        {
            TrafficHouse.Esportiva => level.TrafficCpaEsportiva,
            TrafficHouse.Stake => level.TrafficCpaStake,
            TrafficHouse.Betano => level.TrafficCpaBetano,
            TrafficHouse.BetMgm => level.TrafficCpaBetMgm,
            TrafficHouse.Novibet => level.TrafficCpaNovibet,
            TrafficHouse.BetFair => level.TrafficCpaBetFair,
            TrafficHouse.Blaze => level.TrafficCpaBlaze,
            TrafficHouse.Superbet => level.TrafficCpaSuperbet,
            TrafficHouse.Hiperbet => level.TrafficCpaHiperbet,
            _ => throw new ArgumentOutOfRangeException(nameof(houseKey), houseKey, null)
        };
}
