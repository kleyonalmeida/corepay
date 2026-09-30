namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Resultado agregado do recálculo em lote de uma folha (Fase 5.11).
/// </summary>
public sealed record PayrollBatchResult
{
    public IReadOnlyList<PayrollEntryResult> EntryResults { get; init; } = [];

    /// <summary>Soma dos <see cref="PayrollEntryResult.TotalAmount"/> por colaborador.</summary>
    public decimal TotalAmount { get; init; }
}
