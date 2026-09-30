using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class TrafficFixedAllocationCalculatorTests
{
    [Fact]
    public void Calculate_ShouldSplitEqually_AmongRateioProjects()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var result = TrafficFixedAllocationCalculator.Calculate(
            1000m,
            [
                new RateioProjectEntryInput(projectA),
                new RateioProjectEntryInput(projectB)
            ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Sum(allocation => allocation.Amount).Should().Be(1000m);
        result.Value.Should().Contain(allocation => allocation.ProjectId == projectA && allocation.Amount == 500m);
        result.Value.Should().Contain(allocation => allocation.ProjectId == projectB && allocation.Amount == 500m);
    }

    [Fact]
    public void Calculate_ShouldFail_WhenManualRateioValueIsProvided()
    {
        var result = TrafficFixedAllocationCalculator.Calculate(
            1000m,
            [new RateioProjectEntryInput(Guid.NewGuid(), RateioValue: 600m)]);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.manual_rateio_not_allowed");
    }

    [Fact]
    public void Calculate_ShouldReturnEmpty_WhenFixedIsZeroOrNoProjects()
    {
        TrafficFixedAllocationCalculator.Calculate(0m, [new RateioProjectEntryInput(Guid.NewGuid())])
            .Value.Should().BeEmpty();

        TrafficFixedAllocationCalculator.Calculate(1000m, [])
            .Value.Should().BeEmpty();
    }
}
