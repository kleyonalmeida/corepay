using BuildingBlocks.Results;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorPaidTrafficTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldProduceSameTotal_WhenDepartmentNameIsLegacyOrRenamed()
    {
        var legacy = CalcTrafficWithDepartmentName("Tráfego Pago");
        var renamed = CalcTrafficWithDepartmentName("Marketing Digital XYZ");

        legacy.IsSuccess.Should().BeTrue();
        renamed.IsSuccess.Should().BeTrue();
        renamed.Value.Should().BeEquivalentTo(legacy.Value);
        legacy.Value.CommissionAmount.Should().Be(350m);
    }

    [Fact]
    public void CalcEntry_ShouldDispatchPaidTraffic_ByExplicitProfile()
    {
        var department = CreateDepartment();
        department.Name = "Setor Renomeado Sem Regex";

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Cargo Renomeado",
            Profile = CalculationProfile.PaidTraffic,
            TrafficInvestmentCommissionPct = 2m,
            TrafficCpaBetano = 50m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = department,
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(350m);
    }

    [Fact]
    public void CalcEntry_ShouldNotFail_ForPaidTraffic()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = CreateDepartment(),
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.PaidTraffic,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalcEntry_ShouldDispatchCommercialAnalyst_NotPaidTraffic()
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

    private Result<PayrollEntryResult> CalcTrafficWithDepartmentName(string departmentName)
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = departmentName,
                CalculationType = CalculationProfile.PaidTraffic
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.PaidTraffic,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ]
        };

        return _calculator.CalcEntry(input);
    }

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Tráfego Pago",
            CalculationType = CalculationProfile.PaidTraffic
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Tráfego",
            DepartmentId = Guid.NewGuid()
        };
}
