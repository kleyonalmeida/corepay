using Core.Application.Payrolls;
using Core.Domain.PayrollCalculation;

namespace Core.Application.Finance;

public sealed record FinanceEntryAmounts(
    decimal TotalAmount,
    decimal PlatformTotal,
    decimal AmountToReceive,
    bool IsPaid = false)
{
    public static FinanceEntryAmounts FromResult(PayrollEntryResult? result) =>
        FromAmounts(result?.TotalAmount ?? 0m, result?.PlatformTotal ?? 0m);

    public static FinanceEntryAmounts FromResultResponse(PayrollEntryResultResponse? result) =>
        FromAmounts(result?.TotalAmount ?? 0m, result?.PlatformTotal ?? 0m);

    private static FinanceEntryAmounts FromAmounts(decimal totalAmount, decimal platformTotal)
    {
        var amountToReceive = Math.Max(0m, totalAmount - platformTotal);
        return new FinanceEntryAmounts(totalAmount, platformTotal, amountToReceive);
    }

    public static FinanceGroupAmounts AggregateGroup(IEnumerable<FinanceEntryAmounts> entries)
    {
        var list = entries.ToList();
        var paidEntries = list.Where(e => e.IsPaid).ToList();

        return new FinanceGroupAmounts(
            GrossTotal: list.Sum(e => e.TotalAmount),
            PlatformTotal: list.Sum(e => e.PlatformTotal),
            AmountToReceive: list.Sum(e => e.AmountToReceive),
            PaidAmount: paidEntries.Sum(e => e.AmountToReceive),
            PaidCount: paidEntries.Count,
            EntryCount: list.Count);
    }
}

public sealed record FinanceGroupAmounts(
    decimal GrossTotal,
    decimal PlatformTotal,
    decimal AmountToReceive,
    decimal PaidAmount,
    int PaidCount,
    int EntryCount);
