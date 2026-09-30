using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorFallbackProfilesTests
{
    private readonly PayrollCalculator _calculator = new();

    [Theory]
    [InlineData(CalculationProfile.FixedBonus)]
    [InlineData(CalculationProfile.Tipster)]
    [InlineData(CalculationProfile.AllocatedFixed)]
    public void CalcEntry_ShouldDispatchSection69Profiles_ByExplicitEnumNotByName(CalculationProfile profile)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Nome Renomeado Sem Regex",
            CalculationType = profile,
            GoalBonusPercentage = profile == CalculationProfile.Tipster ? 0m : 10m,
            IsAllocatedFixed = profile == CalculationProfile.AllocatedFixed
        };

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = profile,
            BaseSalary = 3000m,
            GroupCommissionPerPercent = 100m,
            GroupCommissionPer20Percent = 500m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 3000m,
            GoalTier = profile != CalculationProfile.Tipster
                ? GoalTier.Goal
                : GoalTier.None,
            ProjectEntries = profile == CalculationProfile.Tipster
                ? [new ProjectEntryInput(Guid.NewGuid(), 0m, 15m)]
                : [new ProjectEntryInput(Guid.NewGuid(), 0m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().BeGreaterThan(0m);
        result.Value.BaseSalary.Should().Be(3000m);
    }

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_NotFallback()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                CalculationType = CalculationProfile.CommercialAnalyst
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.CommercialAnalyst,
                BaseSalary = 1500m
            },
            Collaborator = CreateCollaborator()
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(1500m);
    }

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador",
            DepartmentId = Guid.NewGuid()
        };
}
