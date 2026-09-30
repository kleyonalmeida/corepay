using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class GetDisplayProjectEntriesTests
{
    private static readonly Guid AffiliatesProjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid LimaKarttosId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherProjectId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid OriginalProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PayingProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ManagementProjectAId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ManagementProjectBId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const int March = 3;
    private const int Year = 2025;

    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void GetDisplayProjectEntries_ShouldAllocateAllCost_ToAffiliatesProject()
    {
        var level = CreateAffiliatesLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateAffiliatesInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [50_000m],
            bonuses: [100m],
            deductions: [50m]);

        var entryResult = _calculator.CalcEntry(input);
        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        entryResult.IsSuccess.Should().BeTrue();
        displayResult.IsSuccess.Should().BeTrue();
        entryResult.Value.TotalAmount.Should().Be(3550m);

        displayResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = AffiliatesProjectId, Amount = 3500m });
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldAllocateFinalSalary_ToAffiliatesProject()
    {
        var level = CreateAffiliatesLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;

        var input = CreateAffiliatesInput(
            goalTier: GoalTier.Goal,
            level: level,
            finalSalary: 4500m,
            projects: [100_000m],
            bonuses: [200m],
            deductions: [100m]);

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = AffiliatesProjectId, Amount = 4500m });
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldNotDependOnProjectOrDepartmentNames_ForAffiliates()
    {
        var level = CreateAffiliatesLevel();
        level.BaseSalary = 1000m;
        level.CommissionWithoutGoalPct = 2m;

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Setor Renomeado Sem Affiliates no Nome",
                CalculationType = CalculationProfile.FixedCommissionBonus
            },
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = GoalTier.None,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 10_000m)]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().ContainSingle()
            .Which.ProjectId.Should().Be(AffiliatesProjectId);
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldCollapseContingencyFixed_ToSingleLimaKarttosLine()
    {
        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Contingência Renomeada",
                CalculationType = CalculationProfile.AllocatedFixed,
                IsAllocatedFixed = true,
                RoutesFixedToLimaKarttos = true
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.AllocatedFixed,
                BaseSalary = 5000m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 5000m,
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)],
            RateioProjectEntries = [new RateioProjectEntryInput(OtherProjectId, null)]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = LimaKarttosId, Amount = 5000m });
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldCollapseAutomationFixed_ToSingleLimaKarttosLine()
    {
        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Automação Renomeada",
                CalculationType = CalculationProfile.AllocatedFixed,
                IsAllocatedFixed = true,
                RoutesFixedToLimaKarttos = true
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.AllocatedFixed,
                BaseSalary = 4200m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 4200m,
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)],
            ProjectEntries =
            [
                new ProjectEntryInput(OtherProjectId, 0m),
                new ProjectEntryInput(Guid.NewGuid(), 0m)
            ]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().ContainSingle()
            .Which.ProjectId.Should().Be(LimaKarttosId);
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldAggregateManagementBreakdown_ByProjectId()
    {
        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
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
            FullBaseSalary = 5000m,
            ManagementRevenueEntries =
            [
                new ManagementRevenueEntryInput(
                    100_000m,
                    [
                        new ManagementProjectBreakdownInput(ManagementProjectAId, 1000m),
                        new ManagementProjectBreakdownInput(ManagementProjectBId, 200m)
                    ]),
                new ManagementRevenueEntryInput(
                    50_000m,
                    [new ManagementProjectBreakdownInput(ManagementProjectAId, 500m)])
            ]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().HaveCount(2);
        displayResult.Value.Should().Contain(t => t.ProjectId == ManagementProjectAId && t.Amount == 1500m);
        displayResult.Value.Should().Contain(t => t.ProjectId == ManagementProjectBId && t.Amount == 200m);
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldPassThroughCommercialTotals_FromPhase61()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            commissionPayingProjectId: PayingProjectId,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 40,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m)
            ]);

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);
        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        totalsResult.IsSuccess.Should().BeTrue();
        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().BeEquivalentTo(totalsResult.Value);
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldPassThroughCommissionOnly_AsEmpty()
    {
        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
                CalculationType = CalculationProfile.CommissionOnly
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.CommissionOnly,
                CommissionWithoutGoalPct = 2m
            },
            Collaborator = CreateCollaborator(),
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 10_000m)]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().BeEmpty();
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldApplyRoleChangeOnce_WithManualBonus()
    {
        var affiliatesDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Parceiros",
            CalculationType = CalculationProfile.FixedCommissionBonus
        };
        var commercialDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

        var affiliatesLevel = CreateAffiliatesLevel();
        affiliatesLevel.BaseSalary = 2000m;
        affiliatesLevel.CommissionWithoutGoalPct = 2m;

        var commercialLevel = CreateCommercialLevel();
        commercialLevel.SalesBonusEvery = 0m;

        var commercialProjectId = Guid.NewGuid();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = affiliatesDepartment,
            CareerLevel = affiliatesLevel,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 2000m,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 10_000m)],
            RoleChanges =
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = commercialDepartment,
                        CareerLevel = commercialLevel,
                        FullBaseSalary = 1500m,
                        CommercialProjectEntries =
                        [
                            new CommercialAnalystProjectEntryInput(
                                commercialProjectId,
                                ProjectPlatform.Lastlink,
                                FtdTotal: 750,
                                FtdSuperbet: 0,
                                false, false, 0,
                                SalesAmount: 0m,
                                false, false,
                                Rev: 0m)
                        ]
                    })
            ],
            BonusEntries = [new BonusEntryInput(commercialProjectId, 100m)],
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)]
        };

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsSuccess.Should().BeTrue();
        displayResult.Value.Should().Contain(t => t.ProjectId == AffiliatesProjectId);
        displayResult.Value.Should().Contain(t => t.ProjectId == commercialProjectId);
        displayResult.Value.Count(t => t.ProjectId == commercialProjectId).Should().Be(1);
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldPropagateComplementValidationFailure()
    {
        var input = CreateCommercialInput(
            complementPayingProjects:
            [
                new ComplementPayingProjectInput(ManagementProjectAId, 60m),
                new ComplementPayingProjectInput(ManagementProjectBId, 30m)
            ],
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    0, 0, false, false, 0,
                    SalesAmount: 25_000m,
                    false, false,
                    0m)
            ]);

        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        displayResult.IsFailure.Should().BeTrue();
        displayResult.Error!.Code.Should().Be("payroll.complement_paying_projects_invalid_sum");
    }

    [Fact]
    public void GetDisplayProjectEntries_ShouldReconcileAffiliatesDisplay_WithEntryTotal()
    {
        var level = CreateAffiliatesLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateAffiliatesInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [10_000m],
            bonuses: [100m, 50m],
            deductions: [75m]);

        var entryResult = _calculator.CalcEntry(input);
        var displayResult = _calculator.GetDisplayProjectEntries(input, AffiliatesProjectId);

        entryResult.IsSuccess.Should().BeTrue();
        displayResult.IsSuccess.Should().BeTrue();

        var unallocatedBonuses = input.BonusEntries
            .Where(b => !b.ProjectId.HasValue)
            .Sum(b => b.Value);
        var deductions = input.DeductionEntries.Sum(d => d.Value);
        var expected = entryResult.Value.TotalAmount + deductions - unallocatedBonuses;

        displayResult.Value.Sum(t => t.Amount).Should().Be(expected);
    }

    private static PayrollEntryInput CreateAffiliatesInput(
        GoalTier goalTier,
        CareerLevel level,
        decimal? finalSalary = null,
        decimal[]? projects = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Parceiros Renomeado",
                CalculationType = CalculationProfile.FixedCommissionBonus
            },
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            GoalTier = goalTier,
            FinalSalary = finalSalary,
            ProjectEntries = (projects ?? []).Select(value =>
                new ProjectEntryInput(Guid.NewGuid(), value)).ToList(),
            BonusEntries = (bonuses ?? []).Select(value =>
                new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value =>
                new DeductionEntryInput(value)).ToList()
        };

    private static CareerLevel CreateAffiliatesLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Affiliate Renomeado",
            Profile = CalculationProfile.FixedCommissionBonus
        };

    private static PayrollEntryInput CreateCommercialInput(
        CareerLevel? level = null,
        Guid? commissionPayingProjectId = null,
        IReadOnlyList<ComplementPayingProjectInput>? complementPayingProjects = null,
        IReadOnlyList<CommercialAnalystProjectEntryInput>? projectEntries = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
                CalculationType = CalculationProfile.CommercialAnalyst
            },
            CareerLevel = level ?? CreateCommercialLevel(),
            Collaborator = CreateCollaborator(),
            CommissionPayingProjectId = commissionPayingProjectId,
            ComplementPayingProjects = complementPayingProjects ?? [],
            CommercialProjectEntries = projectEntries ?? []
        };

    private static CareerLevel CreateCommercialLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Analista Comercial Júnior",
            Profile = CalculationProfile.CommercialAnalyst,
            BaseSalary = 1500m,
            FtdRateBase = 2m,
            FtdRateWithGoal = 2.5m,
            FtdRateWithSuperGoal = 3m,
            FtdSuperbetRate = 5m,
            FtdBonusEvery = 250,
            FtdBonusValue = 350m,
            DefaultCpaValue = 0m,
            SalesPctBase = 4m,
            SalesPctWithGoal = 5m,
            SalesPctWithSuperGoal = 6m,
            SalesBonusEvery = 20_000m,
            SalesBonusValue = 250m,
            RevPct = 1m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}
