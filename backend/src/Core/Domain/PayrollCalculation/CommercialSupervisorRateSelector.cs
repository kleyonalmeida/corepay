namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Seleciona taxas do supervisor comercial conforme metas de FTD e vendas do projeto (REGRAS §6.2).
/// </summary>
public static class CommercialSupervisorRateSelector
{
    public sealed record Rates(
        decimal FtdSuperbetRate,
        decimal FtdOtherRate,
        decimal SalesPct,
        decimal RevPct);

    public static Rates Select(CareerLevel? careerLevel, bool ftdGoal, bool salesGoal)
    {
        if (careerLevel is null)
        {
            return new Rates(0m, 0m, 0m, 0m);
        }

        return new Rates(
            ftdGoal ? careerLevel.SupFtdSuperbetWithGoal : careerLevel.SupFtdSuperbetNoGoal,
            ftdGoal ? careerLevel.SupFtdOtherWithGoal : careerLevel.SupFtdOtherNoGoal,
            salesGoal ? careerLevel.SupSalesPctWithGoal : careerLevel.SupSalesPctNoGoal,
            careerLevel.SupRevPct);
    }
}
