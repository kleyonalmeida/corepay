using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Cashflow;

public class CashflowInstallmentPlannerTests
{
    [Fact]
    public void SplitAmount_ShouldPutRemainderOnLastInstallment()
    {
        var amounts = CashflowInstallmentPlanner.SplitAmount(100m, 3);

        amounts.Should().HaveCount(3);
        amounts.Sum().Should().Be(100m);
        amounts[2].Should().BeGreaterThan(amounts[0]);
    }

    [Fact]
    public void PlanInstallments_ShouldAdvanceMonths()
    {
        var plan = CashflowInstallmentPlanner.PlanInstallments(
            300m,
            3,
            new DateOnly(2026, 1, 31),
            1,
            2026);

        plan.Should().HaveCount(3);
        plan[0].TransactionDate.Should().Be(new DateOnly(2026, 1, 31));
        plan[1].TransactionDate.Should().Be(new DateOnly(2026, 2, 28));
        plan[2].Month.Should().Be(3);
        plan[2].Year.Should().Be(2026);
    }
}
