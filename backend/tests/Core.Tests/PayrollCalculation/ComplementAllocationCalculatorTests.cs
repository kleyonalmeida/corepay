using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ComplementAllocationCalculatorTests
{
    private static readonly Guid LimaKarttosId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PayingProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SplitAId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SplitBId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Allocate_ShouldRouteToLimaKarttos_WhenNoPayingProject()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)]
        };

        var result = ComplementAllocationCalculator.Allocate(500m, input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ProjectTotalAllocation(LimaKarttosId, 500m));
    }

    [Fact]
    public void Allocate_ShouldFail_WhenTargetMissing()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            ProjectSnapshots = []
        };

        var result = ComplementAllocationCalculator.Allocate(500m, input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payroll.complement_allocation_target_missing");
    }

    [Fact]
    public void Allocate_ShouldSplitProportional_60And40()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            ComplementPayingProjects =
            [
                new ComplementPayingProjectInput(SplitAId, 60m),
                new ComplementPayingProjectInput(SplitBId, 40m)
            ]
        };

        var result = ComplementAllocationCalculator.Allocate(1000m, input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(t => t.ProjectId == SplitAId && t.Amount == 600m);
        result.Value.Should().Contain(t => t.ProjectId == SplitBId && t.Amount == 400m);
    }

    [Fact]
    public void Allocate_ShouldUseCommissionPayingProject_WhenSet()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            CommissionPayingProjectId = PayingProjectId
        };

        var result = ComplementAllocationCalculator.Allocate(750m, input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ProjectTotalAllocation(PayingProjectId, 750m));
    }

    [Fact]
    public void Allocate_ShouldReturnEmpty_WhenBucketIsZero()
    {
        var input = new PayrollEntryInput { Month = 3, Year = 2025 };

        var result = ComplementAllocationCalculator.Allocate(0m, input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
