using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class TipsterGroupCommissionCalculatorTests
{
    [Fact]
    public void Calculate_ShouldApplyPerPercentAndSingleVipBand()
    {
        var input = CreateInput(
            groupPercentages: [15m],
            perPercent: 100m,
            perTwenty: 500m);

        var commission = TipsterGroupCommissionCalculator.Calculate(input);

        commission.Should().Be(1500m);
    }

    [Fact]
    public void Calculate_ShouldSumGroupPercentagesAcrossProjects()
    {
        var input = CreateInput(
            groupPercentages: [15m, 25m],
            perPercent: 100m,
            perTwenty: 500m);

        var commission = TipsterGroupCommissionCalculator.Calculate(input);

        commission.Should().Be(5000m);
    }

    [Fact]
    public void Calculate_ShouldApplyOneVipBand_WhenTotalPercentIs39()
    {
        var input = CreateInput(
            groupPercentages: [39m],
            perPercent: 100m,
            perTwenty: 500m);

        var commission = TipsterGroupCommissionCalculator.Calculate(input);

        commission.Should().Be(4400m);
    }

    [Fact]
    public void Calculate_ShouldApplyTwoVipBands_WhenTotalPercentIs40()
    {
        var input = CreateInput(
            groupPercentages: [40m],
            perPercent: 100m,
            perTwenty: 500m);

        var commission = TipsterGroupCommissionCalculator.Calculate(input);

        commission.Should().Be(5000m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoGroupPercentage()
    {
        var input = CreateInput(
            groupPercentages: [0m],
            perPercent: 100m,
            perTwenty: 500m);

        TipsterGroupCommissionCalculator.Calculate(input).Should().Be(0m);
    }

    private static PayrollEntryInput CreateInput(
        decimal[] groupPercentages,
        decimal perPercent,
        decimal perTwenty)
    {
        var projectEntries = groupPercentages
            .Select(percentage => new ProjectEntryInput(Guid.NewGuid(), 0m, percentage))
            .ToList();

        return new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Tipster",
                CalculationType = CalculationProfile.Tipster
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Name = "Tipster",
                Profile = CalculationProfile.Tipster,
                GroupCommissionPerPercent = perPercent,
                GroupCommissionPer20Percent = perTwenty
            },
            Collaborator = new Collaborator
            {
                Id = Guid.NewGuid(),
                Name = "Colaborador",
                DepartmentId = Guid.NewGuid()
            },
            ProjectEntries = projectEntries
        };
    }
}
