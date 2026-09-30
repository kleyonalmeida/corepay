using Core.Application.Finance;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.Finance;

public class FinanceEntryAmountsTests
{
    [Fact]
    public void FromResult_CommercialWithPlatform_ShouldReturnAmountToReceive()
    {
        var result = new PayrollEntryResult
        {
            TotalAmount = 1900m,
            PlatformTotal = 400m
        };

        var amounts = FinanceEntryAmounts.FromResult(result);

        amounts.TotalAmount.Should().Be(1900m);
        amounts.PlatformTotal.Should().Be(400m);
        amounts.AmountToReceive.Should().Be(1500m);
    }

    [Fact]
    public void FromResult_PlatformEqualsGross_ShouldReturnZeroToReceive()
    {
        var result = new PayrollEntryResult
        {
            TotalAmount = 2000m,
            PlatformTotal = 2000m
        };

        var amounts = FinanceEntryAmounts.FromResult(result);

        amounts.AmountToReceive.Should().Be(0m);
    }

    [Fact]
    public void FromResult_NonCommercial_ShouldReturnFullAmountToReceive()
    {
        var result = new PayrollEntryResult
        {
            TotalAmount = 3500m,
            PlatformTotal = 0m
        };

        var amounts = FinanceEntryAmounts.FromResult(result);

        amounts.AmountToReceive.Should().Be(3500m);
    }

    [Fact]
    public void FromResult_NullResult_ShouldReturnZeros()
    {
        var amounts = FinanceEntryAmounts.FromResult(null);

        amounts.TotalAmount.Should().Be(0m);
        amounts.PlatformTotal.Should().Be(0m);
        amounts.AmountToReceive.Should().Be(0m);
    }

    [Fact]
    public void AggregateGroup_ShouldSumVisibleEntriesAndPaidAmount()
    {
        var entries = new[]
        {
            new FinanceEntryAmounts(1900m, 400m, 1500m, true),
            new FinanceEntryAmounts(3000m, 0m, 3000m, false),
            new FinanceEntryAmounts(800m, 200m, 600m, true)
        };

        var aggregate = FinanceEntryAmounts.AggregateGroup(entries);

        aggregate.GrossTotal.Should().Be(5700m);
        aggregate.PlatformTotal.Should().Be(600m);
        aggregate.AmountToReceive.Should().Be(5100m);
        aggregate.PaidAmount.Should().Be(2100m);
        aggregate.PaidCount.Should().Be(2);
        aggregate.EntryCount.Should().Be(3);
    }
}
