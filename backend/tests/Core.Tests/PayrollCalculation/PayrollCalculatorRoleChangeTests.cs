using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorRoleChangeTests
{
    private const int March = 3;
    private const int Year = 2025;

    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcEntry_ShouldApplyFifteenAndSixteenOverThirtyOne_WithSingleDeduction()
    {
        var departmentA = CreateFixedBonusDepartment("Comercial A");
        var departmentB = CreateFixedBonusDepartment("Gerência B");

        var levelA = CreateFixedBonusLevel(3100m);
        var levelB = CreateFixedBonusLevel(3100m);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "Maria",
            DepartmentId = departmentA.Id,
            AdmissionDate = new DateOnly(Year, March, 1)
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = departmentA,
            CareerLevel = levelA,
            Collaborator = collaborator,
            FullBaseSalary = 3100m,
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = departmentB,
                        CareerLevel = levelB,
                        FullBaseSalary = 3100m
                    })
            ],
            DeductionEntries = [new DeductionEntryInput(100m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(3100m);
        result.Value.TotalAmount.Should().Be(3000m);
        collaborator.DismissalDate.Should().BeNull();
    }

    [Fact]
    public void CalcEntry_ShouldUseDifferentProfilesPerPeriod()
    {
        var fixedDepartment = CreateFixedBonusDepartment("Administrativo");
        var commissionDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommissionOnly
        };

        var fixedLevel = CreateFixedBonusLevel(3100m);
        var commissionLevel = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommissionOnly,
            CommissionWithoutGoalPct = 2m
        };

        var projectId = Guid.NewGuid();
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "João",
            DepartmentId = fixedDepartment.Id,
            AdmissionDate = new DateOnly(Year, March, 1)
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = fixedDepartment,
            CareerLevel = fixedLevel,
            Collaborator = collaborator,
            FullBaseSalary = 3100m,
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = commissionDepartment,
                        CareerLevel = commissionLevel,
                        ProjectEntries =
                        [
                            new ProjectEntryInput(projectId, 100_000m)
                        ]
                    })
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        var expectedFixed = decimal.Round(3100m * (15m / 31m), 2, MidpointRounding.AwayFromZero);
        var expectedCommission = 2000m;
        result.Value.BaseSalary.Should().Be(expectedFixed);
        result.Value.CommissionAmount.Should().Be(expectedCommission);
        result.Value.TotalAmount.Should().Be(expectedFixed + expectedCommission);
    }

    [Fact]
    public void CalcEntry_ShouldPropagateFailure_WhenSecondPeriodHasInvalidTrafficInput()
    {
        var fixedDepartment = CreateFixedBonusDepartment("Administrativo");
        var trafficDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Tráfego",
            CalculationType = CalculationProfile.PaidTraffic
        };

        var fixedLevel = CreateFixedBonusLevel(3100m);
        var trafficLevel = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.PaidTraffic,
            BaseSalary = 2000m,
            TrafficInvestmentCommissionPct = 2m
        };

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "Carlos",
            DepartmentId = fixedDepartment.Id,
            AdmissionDate = new DateOnly(Year, March, 1)
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = fixedDepartment,
            CareerLevel = fixedLevel,
            Collaborator = collaborator,
            FullBaseSalary = 3100m,
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = trafficDepartment,
                        CareerLevel = trafficLevel,
                        RateioProjectEntries =
                        [
                            new RateioProjectEntryInput(Guid.NewGuid(), RateioValue: 500m)
                        ]
                    })
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.manual_rateio_not_allowed");
    }

    [Fact]
    public void CalcEntry_ShouldBehaveAsBefore_WhenRoleChangesAreEmpty()
    {
        var department = CreateFixedBonusDepartment("Administrativo");
        var level = CreateFixedBonusLevel(3100m);
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "Ana",
            DepartmentId = department.Id
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = level,
            Collaborator = collaborator,
            FullBaseSalary = 3100m
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(3100m);
        result.Value.TotalAmount.Should().Be(3100m);
    }

    [Fact]
    public void CalcEntry_ShouldIgnoreRoleChangesOutsideReferenceMonth()
    {
        var department = CreateFixedBonusDepartment("Administrativo");
        var level = CreateFixedBonusLevel(3100m);
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "Paula",
            DepartmentId = department.Id
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = level,
            Collaborator = collaborator,
            FullBaseSalary = 3100m,
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, 4, 1),
                    new PayrollRoleSnapshot
                    {
                        Department = department,
                        CareerLevel = level,
                        FullBaseSalary = 5000m
                    })
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(3100m);
    }

    private static Department CreateFixedBonusDepartment(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CalculationType = CalculationProfile.FixedBonus
        };

    private static CareerLevel CreateFixedBonusLevel(decimal baseSalary) =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.FixedBonus,
            BaseSalary = baseSalary
        };
}
