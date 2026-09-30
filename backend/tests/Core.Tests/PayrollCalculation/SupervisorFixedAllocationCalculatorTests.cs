using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class SupervisorFixedAllocationCalculatorTests
{
    [Fact]
    public void Calculate_ShouldAllocateFixedOnlyToEligibleProjects()
    {
        var threeCSports = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var otherProject = Guid.NewGuid();

        var result = SupervisorFixedAllocationCalculator.Calculate(
            3000m,
            [
                new SupervisorProjectEntryInput(
                    threeCSports, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(
                    otherProject, 0, 0, 0m, 0m, false, false)
            ],
            [
                new ProjectCalculationSnapshot(threeCSports, ExcludesSupervisorFixedAllocation: true),
                new ProjectCalculationSnapshot(otherProject)
            ]);

        result.Should().HaveCount(1);
        result[0].ProjectId.Should().Be(otherProject);
        result[0].Amount.Should().Be(3000m);
    }

    [Fact]
    public void Calculate_ShouldSplitEqually_AmongEligibleProjects()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var result = SupervisorFixedAllocationCalculator.Calculate(
            1000m,
            [
                new SupervisorProjectEntryInput(projectA, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectB, 0, 0, 0m, 0m, false, false)
            ],
            [
                new ProjectCalculationSnapshot(projectA),
                new ProjectCalculationSnapshot(projectB)
            ]);

        result.Should().HaveCount(2);
        result.Sum(allocation => allocation.Amount).Should().Be(1000m);
        result.Should().Contain(allocation => allocation.ProjectId == projectA && allocation.Amount == 500m);
        result.Should().Contain(allocation => allocation.ProjectId == projectB && allocation.Amount == 500m);
    }

    [Fact]
    public void Calculate_ShouldReturnEmpty_WhenFixedIsZero()
    {
        var result = SupervisorFixedAllocationCalculator.Calculate(
            0m,
            [new SupervisorProjectEntryInput(Guid.NewGuid(), 0, 0, 0m, 0m, false, false)],
            []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ShouldReturnEmpty_WhenAllProjectsExcludeFixed()
    {
        var threeCSports = Guid.NewGuid();

        var result = SupervisorFixedAllocationCalculator.Calculate(
            3000m,
            [new SupervisorProjectEntryInput(threeCSports, 0, 0, 0m, 0m, false, false)],
            [new ProjectCalculationSnapshot(threeCSports, ExcludesSupervisorFixedAllocation: true)]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ShouldDistributeRemainderOnLastEligibleProject()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var projectC = Guid.NewGuid();

        var result = SupervisorFixedAllocationCalculator.Calculate(
            100m,
            [
                new SupervisorProjectEntryInput(projectA, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectB, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectC, 0, 0, 0m, 0m, false, false)
            ],
            [
                new ProjectCalculationSnapshot(projectA),
                new ProjectCalculationSnapshot(projectB),
                new ProjectCalculationSnapshot(projectC)
            ]);

        result.Sum(allocation => allocation.Amount).Should().Be(100m);
        result[^1].Amount.Should().Be(33.34m);
    }
}
