namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Mudança de cargo dentro da competência (REGRAS §5.2).
/// </summary>
public sealed record RoleChangeEntryInput(
    DateOnly ChangeDate,
    PayrollRoleSnapshot Role);
