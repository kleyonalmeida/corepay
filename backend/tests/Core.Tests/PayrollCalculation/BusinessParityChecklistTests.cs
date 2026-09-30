using BuildingBlocks.Results;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

/// <summary>
/// Checklist §15.4 de REGRAS_DE_NEGOCIO.md — invariantes transversais de paridade de negócio (Fase 15.4).
/// Perfis individuais permanecem nos *CalculatorTests e PayrollCalculator*Tests especializados.
/// </summary>
[Trait("Category", "BusinessParity")]
public class BusinessParityChecklistTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void Checklist_RoleChange_ShouldTakePrecedenceOverExplicitProfile()
    {
        var commercialDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };
        var fixedDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Administrativo",
            CalculationType = CalculationProfile.FixedBonus,
            GoalBonusPercentage = 0m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = commercialDepartment,
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.CommercialAnalyst,
                BaseSalary = 1500m,
                FtdRateBase = 2m,
                SalesPctBase = 4m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1500m,
            CommercialProjectEntries =
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    FtdTotal: 100,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m)
            ],
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(2025, 3, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = fixedDepartment,
                        CareerLevel = new CareerLevel
                        {
                            Id = Guid.NewGuid(),
                            Profile = CalculationProfile.FixedBonus,
                            BaseSalary = 3100m
                        },
                        FullBaseSalary = 3100m,
                        ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 0m)]
                    })
            ],
            DeductionEntries = [new DeductionEntryInput(50m)]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Checklist_ProfileResolution_ShouldPreferOverrideThenLevelThenDepartment()
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Setor Renomeado",
            CalculationType = CalculationProfile.CommercialAnalyst
        };
        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Nível Renomeado",
            Profile = CalculationProfile.PaidTraffic
        };
        var collaborator = CreateCollaborator();
        collaborator.CalculationProfileOverride = CalculationProfile.Management;

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.Management);
        CalculationProfileResolver.Resolve(
                new Collaborator { Id = Guid.NewGuid(), DepartmentId = department.Id },
                department,
                level)
            .Should().Be(CalculationProfile.PaidTraffic);
        CalculationProfileResolver.Resolve(
                new Collaborator { Id = Guid.NewGuid(), DepartmentId = department.Id },
                department,
                careerLevel: null)
            .Should().Be(CalculationProfile.CommercialAnalyst);
    }

    [Fact]
    public void Checklist_RenamedPaidTrafficDepartment_ShouldKeepCommission350()
    {
        var baseline = CalcPaidTrafficCommission(departmentName: "Tráfego Pago");
        var renamed = CalcPaidTrafficCommission(departmentName: "Marketing Digital XYZ");

        baseline.IsSuccess.Should().BeTrue();
        renamed.IsSuccess.Should().BeTrue();
        renamed.Value.CommissionAmount.Should().Be(baseline.Value.CommissionAmount);
        renamed.Value.CommissionAmount.Should().Be(350m);
    }

    [Fact]
    public void Checklist_RecalcAllEntries_ShouldSumFixtureProfilesFromPhase03()
    {
        var commercial = CreateCommercialFixtureInput();
        var affiliates = CreateAffiliatesFixtureInput();
        var traffic = CreatePaidTrafficFixtureInput();

        var singles = new[]
        {
            _calculator.CalcEntry(commercial).Value,
            _calculator.CalcEntry(affiliates).Value,
            _calculator.CalcEntry(traffic).Value
        };

        var batch = _calculator.RecalcAllEntries([commercial, affiliates, traffic]);

        batch.IsSuccess.Should().BeTrue();
        batch.Value.TotalAmount.Should().Be(singles.Sum(r => r.TotalAmount));
        batch.Value.EntryResults.Should().HaveCount(3);
    }

    [Fact]
    public void Checklist_Management_ShouldUseExplicitPercentPoints_NotLegacyDecimal()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Gerência",
                CalculationType = CalculationProfile.Management
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.Management,
                BaseSalary = 5000m,
                NetRevenueFactor = 50m,
                NetRevenuePctNoGoal = 2m
            },
            Collaborator = CreateCollaborator(),
            ManagementRevenueEntries =
            [
                new ManagementRevenueEntryInput(100_000m, [])
            ]
        };

        var result = _calculator.CalcEntry(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(1000m);
    }

    [Fact]
    public void Checklist_PaidTraffic_ShouldIgnoreTrafficSupFields()
    {
        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.PaidTraffic,
            TrafficInvestmentCommissionPct = 2m,
            TrafficCpaBetano = 50m,
            TrafficSupBonus = 999m,
            TrafficSupCommissionPct = 99m
        };

        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Tráfego",
                CalculationType = CalculationProfile.PaidTraffic
            },
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

    private Result<PayrollEntryResult> CalcPaidTrafficCommission(string departmentName)
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
                Name = "Sênior Renomeado",
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

    private static PayrollEntryInput CreateCommercialFixtureInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Analistas Comerciais",
                CalculationType = CalculationProfile.CommercialAnalyst
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Name = "Analista Comercial Júnior",
                Profile = CalculationProfile.CommercialAnalyst,
                BaseSalary = 1500m,
                FtdRateBase = 2m,
                SalesPctBase = 4m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1500m,
            CommercialProjectEntries =
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    FtdTotal: 0,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 25_000m,
                    false, false,
                    Rev: 0m)
            ]
        };

    private static PayrollEntryInput CreateAffiliatesFixtureInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Affiliates",
                CalculationType = CalculationProfile.FixedCommissionBonus
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.FixedCommissionBonus,
                BaseSalary = 2000m,
                CommissionWithoutGoalPct = 2m,
                GoalBonusValue = 500m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 2000m,
            GoalTier = GoalTier.Goal,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 50_000m)]
        };

    private static PayrollEntryInput CreatePaidTrafficFixtureInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Tráfego Pago",
                CalculationType = CalculationProfile.PaidTraffic
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Name = "Sênior",
                Profile = CalculationProfile.PaidTraffic,
                BaseSalary = 1000m,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1000m,
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ]
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}
