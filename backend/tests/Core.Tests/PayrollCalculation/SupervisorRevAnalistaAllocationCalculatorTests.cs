using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class SupervisorRevAnalistaAllocationCalculatorTests
{
    [Fact]
    public void Calculate_ShouldSplitEqually_Including3CSports()
    {
        var threeCSports = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var otherProject = Guid.NewGuid();

        var result = SupervisorRevAnalistaAllocationCalculator.Calculate(
            200m,
            [
                new SupervisorProjectEntryInput(
                    threeCSports, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(
                    otherProject, 0, 0, 0m, 0m, false, false)
            ]);

        result.Should().HaveCount(2);
        result.Sum(allocation => allocation.Amount).Should().Be(200m);
        result.Should().Contain(allocation => allocation.ProjectId == threeCSports && allocation.Amount == 100m);
        result.Should().Contain(allocation => allocation.ProjectId == otherProject && allocation.Amount == 100m);
    }

    [Fact]
    public void Calculate_ShouldReturnEmpty_WhenRevIsZeroOrNoProjects()
    {
        SupervisorRevAnalistaAllocationCalculator.Calculate(0m, [])
            .Should().BeEmpty();

        SupervisorRevAnalistaAllocationCalculator.Calculate(
            100m,
            []).Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ShouldDistributeRemainderOnLastProject()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var projectC = Guid.NewGuid();

        var result = SupervisorRevAnalistaAllocationCalculator.Calculate(
            100m,
            [
                new SupervisorProjectEntryInput(projectA, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectB, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectC, 0, 0, 0m, 0m, false, false)
            ]);

        result.Sum(allocation => allocation.Amount).Should().Be(100m);
        result[^1].Amount.Should().Be(33.34m);
    }
}
