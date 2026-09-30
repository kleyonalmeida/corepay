using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ProjectTotalsMergerTests
{
    private static readonly Guid ProjectA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProjectB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Merge_ShouldSumAmounts_ForSameProjectId()
    {
        var merged = ProjectTotalsMerger.Merge(
        [
            [new ProjectTotalAllocation(ProjectA, 100m), new ProjectTotalAllocation(ProjectB, 50m)],
            [new ProjectTotalAllocation(ProjectA, 25m)]
        ]);

        merged.Should().HaveCount(2);
        merged.Should().Contain(t => t.ProjectId == ProjectA && t.Amount == 125m);
        merged.Should().Contain(t => t.ProjectId == ProjectB && t.Amount == 50m);
    }

    [Fact]
    public void ApplyManualBonuses_ShouldAddOnlyBonusesWithProjectId()
    {
        var accumulator = new ProjectTotalsAccumulator();
        accumulator.Add(ProjectA, 100m);

        ProjectTotalsMerger.ApplyManualBonuses(
            accumulator,
            [
                new BonusEntryInput(ProjectB, 50m),
                new BonusEntryInput(null, 999m)
            ]);

        var result = accumulator.ToList();

        result.Should().Contain(t => t.ProjectId == ProjectA && t.Amount == 100m);
        result.Should().Contain(t => t.ProjectId == ProjectB && t.Amount == 50m);
        result.Sum(t => t.Amount).Should().Be(150m);
    }
}
