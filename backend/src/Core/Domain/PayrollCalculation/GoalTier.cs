namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Faixa de meta atingida na entrada da folha.
/// Perfis binários (§6.6–6.8) tratam <see cref="Goal"/> e <see cref="SuperGoal"/> como meta atingida;
/// apenas <see cref="ProjectLeader"/> (§6.5) distingue as três faixas.
/// </summary>
public enum GoalTier
{
    None = 0,
    Goal = 1,
    SuperGoal = 2
}
