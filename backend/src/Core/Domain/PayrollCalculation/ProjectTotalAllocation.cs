namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo alocado a um projeto para uma linha de colaborador (Fase 6.1).
/// </summary>
public sealed record ProjectTotalAllocation(
    Guid ProjectId,
    decimal Amount,
    decimal BaseSalary = 0m,
    decimal Commission = 0m,
    decimal GoalBonus = 0m,
    decimal ManualBonus = 0m,
    decimal Other = 0m);
