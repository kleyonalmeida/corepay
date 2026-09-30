using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorCommercialSupervisorTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialSupervisor_ByExplicitProfile()
    {
        var department = CreateDepartment();
        department.Name = "Nome Renomeado Sem Regex";

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = CalculationProfile.CommercialSupervisor,
            BaseSalary = 2000m,
            SupFtdSuperbetNoGoal = 4m,
            SupFtdOtherNoGoal = 0.3m,
            SupSalesPctNoGoal = 0.5m,
            SupRevPct = 10m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            SupervisorAnalystRevenue = 100m,
            SupervisorProjectEntries =
            [
                new SupervisorProjectEntryInput(
                    Guid.NewGuid(), 10, 3, 0m, 0m, false, false)
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(2000m);
        result.Value.CommissionAmount.Should().Be(14.1m + 100m);
        result.Value.TotalAmount.Should().Be(2000m + 14.1m + 100m);
    }

    [Fact]
    public void CalcEntry_ShouldNotThrow_ForCommercialSupervisor()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = CreateDepartment(),
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.CommercialSupervisor,
                BaseSalary = 1000m
            },
            Collaborator = CreateCollaborator()
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_NotSupervisor()
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
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor Comercial",
            DepartmentId = Guid.NewGuid()
        };
}
