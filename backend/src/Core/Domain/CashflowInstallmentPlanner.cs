namespace Core.Domain;

public static class CashflowInstallmentPlanner
{
    public static IReadOnlyList<(int InstallmentNumber, decimal Amount, DateOnly TransactionDate, int Month, int Year)> PlanInstallments(
        decimal totalAmount,
        int installmentTotal,
        DateOnly firstTransactionDate,
        int firstMonth,
        int firstYear)
    {
        if (installmentTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentTotal));
        }

        if (installmentTotal == 1)
        {
            return
            [
                (1, totalAmount, firstTransactionDate, firstMonth, firstYear)
            ];
        }

        var amounts = SplitAmount(totalAmount, installmentTotal);
        var result = new List<(int, decimal, DateOnly, int, int)>(installmentTotal);

        for (var installment = 1; installment <= installmentTotal; installment++)
        {
            var transactionDate = firstTransactionDate.AddMonths(installment - 1);
            var month = firstMonth;
            var year = firstYear;

            if (installment > 1)
            {
                var shifted = ShiftCompetence(firstMonth, firstYear, installment - 1);
                month = shifted.Month;
                year = shifted.Year;
            }

            result.Add((installment, amounts[installment - 1], transactionDate, month, year));
        }

        return result;
    }

    public static decimal[] SplitAmount(decimal totalAmount, int installmentTotal)
    {
        var totalCents = (long)Math.Round(totalAmount * 100m, MidpointRounding.AwayFromZero);
        var baseCents = totalCents / installmentTotal;
        var amounts = new decimal[installmentTotal];

        decimal allocated = 0m;
        for (var i = 0; i < installmentTotal - 1; i++)
        {
            amounts[i] = baseCents / 100m;
            allocated += amounts[i];
        }

        amounts[installmentTotal - 1] = totalAmount - allocated;
        return amounts;
    }

    private static (int Month, int Year) ShiftCompetence(int month, int year, int monthsToAdd)
    {
        var date = new DateOnly(year, month, 1).AddMonths(monthsToAdd);
        return (date.Month, date.Year);
    }
}
