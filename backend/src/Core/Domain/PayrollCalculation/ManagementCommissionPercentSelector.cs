namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Seleciona o percentual de comissão sobre receita líquida conforme meta (Gerência §6.4).
/// </summary>
public static class ManagementCommissionPercentSelector
{
    public static decimal Select(CareerLevel? careerLevel, GoalTier goalTier)
    {
        if (careerLevel is null)
        {
            return 0m;
        }

        return goalTier != GoalTier.None
            ? careerLevel.NetRevenuePctWithGoal
            : careerLevel.NetRevenuePctNoGoal;
    }
}
