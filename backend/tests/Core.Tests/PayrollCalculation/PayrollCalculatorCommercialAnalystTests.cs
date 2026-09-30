using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorCommercialAnalystTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_ByExplicitProfile()
    {
        var department = CreateDepartment();
        department.Name = "Nome Renomeado Sem Regex";

        var level = CreateLevel();
        level.Name = "Cargo Renomeado";

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1500m,
            CommercialProjectEntries =
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    FtdTotal: 100,
                    FtdSuperbet: 10,
                    false, false, 0,
                    SalesAmount: 10_000m,
                    false, false,
                    Rev: 0m)
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(630m);
        result.Value.BaseSalary.Should().Be(870m);
        result.Value.PlatformTotal.Should().Be(400m);
        result.Value.TotalAmount.Should().Be(1900m);
    }

    [Fact]
    public void CalcEntry_ShouldNotThrow_ForCommercialAnalyst()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = CreateDepartment(),
            CareerLevel = CreateLevel(),
            Collaborator = CreateCollaborator()
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
    }

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommercialAnalyst,
            BaseSalary = 1500m,
            FtdRateBase = 2m,
            FtdSuperbetRate = 5m,
            SalesPctBase = 4m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Ana Comercial",
            DepartmentId = Guid.NewGuid()
        };
}
