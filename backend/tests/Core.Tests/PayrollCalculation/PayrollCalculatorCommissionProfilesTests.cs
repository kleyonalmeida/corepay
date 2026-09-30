using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorCommissionProfilesTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldReturnZero_WhenDepartmentIsMissing()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = null,
            Collaborator = CreateCollaborator()
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(PayrollEntryResult.Zero);
    }

    [Theory]
    [InlineData(CalculationProfile.CommissionOnly)]
    [InlineData(CalculationProfile.FixedCommission)]
    [InlineData(CalculationProfile.FixedCommissionBonus)]
    public void CalcEntry_ShouldDispatchByExplicitProfile_NotByName(CalculationProfile profile)
    {
        var department = CreateDepartment(profile);
        department.Name = "Nome Renomeado Sem Regex";

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = profile,
            BaseSalary = profile == CalculationProfile.CommissionOnly ? 0m : 1000m,
            CommissionWithoutGoalPct = 2m,
            CommissionWithGoalPct = 2m,
            GoalBonusValue = profile == CalculationProfile.FixedCommissionBonus ? 500m : 0m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = profile == CalculationProfile.FixedCommissionBonus
                ? GoalTier.Goal
                : GoalTier.None,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 10_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CalcEntry_ShouldUseCommissionOnlyBranch_NotFixedCommissionBonus()
    {
        var department = CreateDepartment(CalculationProfile.CommissionOnly);
        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Comissão Pura",
            Profile = CalculationProfile.CommissionOnly,
            CommissionWithoutGoalPct = 2m,
            CommissionWithGoalPct = 2m,
            GoalBonusValue = 999m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = GoalTier.Goal,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 10_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(200m);
        result.Value.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public void CalcEntry_ShouldSucceed_ForCommercialAnalyst()
    {
        var department = CreateDepartment(CalculationProfile.CommercialAnalyst);
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
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

    private static Department CreateDepartment(CalculationProfile profile) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Setor Teste",
            CalculationType = profile
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}
