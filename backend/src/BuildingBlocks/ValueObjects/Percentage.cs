namespace BuildingBlocks.ValueObjects;

public readonly record struct Percentage(decimal PercentPoints)
{
    public static Percentage FromPercentPoints(decimal percentPoints) => new(percentPoints);

    public decimal ToFactor() => PercentPoints / 100m;

    public Money ApplyTo(Money money) => money * ToFactor();
}
