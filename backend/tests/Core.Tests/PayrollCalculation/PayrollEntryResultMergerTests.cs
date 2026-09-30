using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollEntryResultMergerTests
{
    [Fact]
    public void Merge_ShouldSumAllFields_AndApplyManualAdjustmentsOnce()
    {
        var periods = new[]
        {
            new PayrollEntryResult
            {
                TotalAmount = 1500m,
                BaseSalary = 1500m,
                CommissionAmount = 0m,
                GoalBonusAmount = 0m,
                GroupCommissionAmount = 0m,
                PlatformTotal = 0m
            },
            new PayrollEntryResult
            {
                TotalAmount = 1600m,
                BaseSalary = 1600m,
                CommissionAmount = 0m,
                GoalBonusAmount = 0m,
                GroupCommissionAmount = 0m,
                PlatformTotal = 0m
            }
        };

        var merged = PayrollEntryResultMerger.Merge(periods, manualBonuses: 0m, manualDeductions: 100m);

        merged.BaseSalary.Should().Be(3100m);
        merged.TotalAmount.Should().Be(3000m);
    }

    [Fact]
    public void Merge_ShouldAddManualBonusesOnce()
    {
        var periods = new[]
        {
            new PayrollEntryResult { TotalAmount = 1000m, BaseSalary = 1000m }
        };

        var merged = PayrollEntryResultMerger.Merge(periods, manualBonuses: 50m, manualDeductions: 0m);

        merged.TotalAmount.Should().Be(1050m);
    }
}
