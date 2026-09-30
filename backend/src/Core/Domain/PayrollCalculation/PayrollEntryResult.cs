namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Resultado do cálculo de uma linha de colaborador.
/// </summary>
public sealed record PayrollEntryResult
{
    public decimal TotalAmount { get; init; }

    /// <summary>Fixo proporcional (0 em comissão pura).</summary>
    public decimal BaseSalary { get; init; }

    /// <summary>Comissão percentual sobre faturamento (perfis §6.6–6.7).</summary>
    public decimal CommissionAmount { get; init; }

    /// <summary>Bônus de meta (% do setor — §6.9).</summary>
    public decimal GoalBonusAmount { get; init; }

    /// <summary>Comissão de grupo Tipster (R$ por % + VIP — §6.9).</summary>
    public decimal GroupCommissionAmount { get; init; }

    /// <summary>Valor pago pela Lastlink/Hubla — só Analista Comercial (§5.4).</summary>
    public decimal PlatformTotal { get; init; }

    public static PayrollEntryResult Zero => new()
    {
        TotalAmount = 0m,
        BaseSalary = 0m,
        CommissionAmount = 0m,
        GoalBonusAmount = 0m,
        GroupCommissionAmount = 0m,
        PlatformTotal = 0m
    };
}
