namespace BuildingBlocks.ValueObjects;

public readonly record struct Money(decimal Amount, Currency Currency = Currency.Brl)
{
    public static Money FromDecimal(decimal amount) => new(amount);

    public Money RoundToCurrencyScale() =>
        new(decimal.Round(Amount, 2, MidpointRounding.AwayFromZero), Currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier) =>
        new(money.Amount * multiplier, money.Currency);

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException("Money operations require the same currency.");
        }
    }
}
