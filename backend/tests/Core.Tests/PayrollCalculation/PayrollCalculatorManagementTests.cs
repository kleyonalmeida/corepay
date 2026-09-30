using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorManagementTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldDispatchManagement_ByExplicitProfile()
    {
        var department = CreateDepartment();
        department.Name = "Nome Renomeado Sem Regex";

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = CalculationProfile.Management,
            BaseSalary = 1000m,
            NetRevenueFactor = 50m,
            NetRevenuePctNoGoal = 2m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = GoalTier.None,
            ManagementRevenueEntries = [new ManagementRevenueEntryInput(100_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(2000m);
        result.Value.BaseSalary.Should().Be(1000m);
        result.Value.CommissionAmount.Should().Be(1000m);
    }

    [Fact]
    public void CalcEntry_ShouldNotThrow_ForManagement()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = CreateDepartment(),
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.Management,
                NetRevenueFactor = 50m,
                NetRevenuePctNoGoal = 2m
            },
            Collaborator = CreateCollaborator(),
            ManagementRevenueEntries = [new ManagementRevenueEntryInput(100_000m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_NotManagement()
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
            Name = "Gerência",
            CalculationType = CalculationProfile.Management
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Gerência",
            DepartmentId = Guid.NewGuid()
        };
}
