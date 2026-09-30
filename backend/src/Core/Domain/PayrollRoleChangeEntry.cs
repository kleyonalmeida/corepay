namespace Core.Domain;

/// <summary>
/// Mudança de cargo persistida na competência (REGRAS §5.2).
/// </summary>
public sealed class PayrollRoleChangeEntry
{
    public DateOnly ChangeDate { get; set; }

    public PayrollRoleChangeSnapshot Role { get; set; } = new();
}
