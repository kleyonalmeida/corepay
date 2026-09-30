using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class FixedAllocationCalculatorTests
{
    private static readonly Guid FeiraId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid LimaKarttosId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Calculate_ShouldSplitEqually_WhenTwoProjectsWithoutManualValues()
    {
        var department = CreateDepartment(isAllocatedFixed: true);
        var snapshots = CreateSnapshots(feira: false, other: false);

        var allocations = FixedAllocationCalculator.Calculate(
            3000m,
            department,
            [],
            [new ProjectEntryInput(FeiraId, 0m), new ProjectEntryInput(OtherId, 0m)],
            snapshots);

        allocations.Should().HaveCount(2);
        allocations.Should().Contain(a => a.ProjectId == FeiraId && a.Amount == 1500m);
        allocations.Should().Contain(a => a.ProjectId == OtherId && a.Amount == 1500m);
    }

    [Fact]
    public void Calculate_ShouldReserveManualValues_AndSplitRemainderEqually()
    {
        var department = CreateDepartment(isAllocatedFixed: true);
        var snapshots = CreateSnapshots(feira: false, other: false);

        var allocations = FixedAllocationCalculator.Calculate(
            3000m,
            department,
            [
                new RateioProjectEntryInput(FeiraId, 2000m),
                new RateioProjectEntryInput(OtherId, null)
            ],
            [],
            snapshots);

        allocations.Should().Contain(a => a.ProjectId == FeiraId && a.Amount == 2000m);
        allocations.Should().Contain(a => a.ProjectId == OtherId && a.Amount == 1000m);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenManualValuesExceedProportionalFixed()
    {
        var department = CreateDepartment(isAllocatedFixed: true);
        var snapshots = CreateSnapshots(feira: false, other: false);

        var act = () => FixedAllocationCalculator.Calculate(
            3000m,
            department,
            [
                new RateioProjectEntryInput(FeiraId, 2000m),
                new RateioProjectEntryInput(OtherId, 1500m)
            ],
            [],
            snapshots);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_ShouldRouteEntireFixedToLimaKarttos_WhenDepartmentRoutesFixed()
    {
        var department = CreateDepartment(routesToLima: true);
        var snapshots = new List<ProjectCalculationSnapshot>
        {
            new(LimaKarttosId, IsDefaultAllocationTarget: true),
            new(OtherId)
        };

        var allocations = FixedAllocationCalculator.Calculate(
            5000m,
            department,
            [new RateioProjectEntryInput(OtherId, null)],
            [new ProjectEntryInput(OtherId, 0m)],
            snapshots);

        allocations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FixedAllocationCalculator.Allocation(LimaKarttosId, 5000m));
    }

    [Fact]
    public void Calculate_ShouldPreferRateioProjectEntries_OverProjectEntries()
    {
        var department = CreateDepartment(isAllocatedFixed: true);
        var thirdId = Guid.NewGuid();
        var snapshots = new List<ProjectCalculationSnapshot>
        {
            new(FeiraId),
            new(OtherId),
            new(thirdId)
        };

        var allocations = FixedAllocationCalculator.Calculate(
            3000m,
            department,
            [new RateioProjectEntryInput(FeiraId, null), new RateioProjectEntryInput(OtherId, null)],
            [new ProjectEntryInput(thirdId, 0m)],
            snapshots);

        allocations.Should().HaveCount(2);
        allocations.Select(a => a.ProjectId).Should().NotContain(thirdId);
    }

    private static Department CreateDepartment(
        bool isAllocatedFixed = false,
        bool routesToLima = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Setor Teste",
            CalculationType = CalculationProfile.AllocatedFixed,
            IsAllocatedFixed = isAllocatedFixed,
            RoutesFixedToLimaKarttos = routesToLima
        };

    private static List<ProjectCalculationSnapshot> CreateSnapshots(bool feira, bool other) =>
    [
        new(FeiraId, ExcludesGoalBonus: feira),
        new(OtherId, ExcludesGoalBonus: other)
    ];
}
