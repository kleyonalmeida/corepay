namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Seleciona o percentual base do Líder de Projetos conforme faixa de meta (REGRAS §6.5).
/// </summary>
public static class ProjectLeaderCommissionPercentSelector
{
    public static decimal Select(CareerLevel? careerLevel, GoalTier goalTier)
    {
        if (careerLevel is null)
        {
            return 0m;
        }

        return goalTier switch
        {
            GoalTier.SuperGoal => careerLevel.CommissionWithSuperGoalPct,
            GoalTier.Goal => careerLevel.CommissionWithGoalPct,
            _ => careerLevel.CommissionWithoutGoalPct
        };
    }
}
