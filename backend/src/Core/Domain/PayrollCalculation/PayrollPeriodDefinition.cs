namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Intervalo inclusivo de cálculo dentro da competência (§5.2).
/// </summary>
public sealed record PayrollPeriodDefinition(
    DateOnly StartInclusive,
    DateOnly EndInclusive,
    PayrollRoleSnapshot Role);
