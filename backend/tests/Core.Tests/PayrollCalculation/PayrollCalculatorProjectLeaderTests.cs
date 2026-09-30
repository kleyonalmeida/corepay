using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorProjectLeaderTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldDispatchProjectLeader_ByExplicitProfile()
    {
        var department = CreateDepartment();
        department.Name = "Nome Renomeado Sem Regex";

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = CalculationProfile.ProjectLeader,
            BaseSalary = 1000m,
            CommissionWithoutGoalPct = 1m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = GoalTier.None,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 100_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(2120m);
        result.Value.BaseSalary.Should().Be(1000m);
        result.Value.CommissionAmount.Should().Be(1120m);
    }

    [Fact]
    public void CalcEntry_ShouldNotThrow_ForProjectLeader()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = CreateDepartment(),
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.ProjectLeader,
                CommissionWithoutGoalPct = 1m
            },
            Collaborator = CreateCollaborator(),
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 300_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_NotProjectLeader()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
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

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Líderes de Projetos",
            CalculationType = CalculationProfile.ProjectLeader,
            LowRevenueThreshold = 200_000m,
            LowRevenueBonusPct = 0.4m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Líder",
            DepartmentId = Guid.NewGuid()
        };
}
