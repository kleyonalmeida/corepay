namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Seleciona o percentual de comissão conforme meta atingida (§6.6–6.8).
/// </summary>
public static class CommissionPercentSelector
{
    public static decimal Select(CareerLevel? careerLevel, GoalTier goalTier)
    {
        if (careerLevel is null)
        {
            return 0m;
        }

        return goalTier != GoalTier.None
            ? careerLevel.CommissionWithGoalPct
            : careerLevel.CommissionWithoutGoalPct;
    }
}
