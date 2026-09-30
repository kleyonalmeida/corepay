namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Seleciona taxas FTD e vendas do analista comercial conforme 0/1/2 metas (REGRAS §6.1).
/// </summary>
public static class CommercialAnalystRateSelector
{
    public sealed record Rates(decimal FtdRate, decimal SalesPct);

    public static Rates SelectFtd(CareerLevel? careerLevel, bool personalGoal, bool projectGoal)
    {
        if (careerLevel is null)
        {
            return new Rates(0m, 0m);
        }

        var goalCount = CountGoals(personalGoal, projectGoal);

        var ftdRate = goalCount switch
        {
            2 => careerLevel.FtdRateWithSuperGoal,
            1 => careerLevel.FtdRateWithGoal,
            _ => careerLevel.FtdRateBase
        };

        return new Rates(ftdRate, 0m);
    }

    public static Rates SelectSales(CareerLevel? careerLevel, bool personalGoal, bool projectGoal)
    {
        if (careerLevel is null)
        {
            return new Rates(0m, 0m);
        }

        var goalCount = CountGoals(personalGoal, projectGoal);

        var salesPct = goalCount switch
        {
            2 => careerLevel.SalesPctWithSuperGoal,
            1 => careerLevel.SalesPctWithGoal,
            _ => careerLevel.SalesPctBase
        };

        return new Rates(0m, salesPct);
    }

    private static int CountGoals(bool personalGoal, bool projectGoal) =>
        (personalGoal ? 1 : 0) + (projectGoal ? 1 : 0);
}
