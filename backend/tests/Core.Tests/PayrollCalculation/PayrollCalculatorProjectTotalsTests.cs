using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorProjectTotalsTests
{
    private static readonly Guid OriginalProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PayingProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SplitProjectAId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SplitProjectBId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LimaKarttosId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FeiraId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ThreeCSportsId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const int March = 3;
    private const int Year = 2025;

    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldRedirectSmallCommercialCommission_ToPayingProject()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 80m,
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

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        entryResult.Value.CommissionAmount.Should().Be(80m);

        totalsResult.Value.Should().Contain(t => t.ProjectId == PayingProjectId && t.Amount == 80m);
        totalsResult.Value.Should().NotContain(t => t.ProjectId == OriginalProjectId);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldAllocateComplement500_ToSinglePayingProject()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 1500m,
            commissionPayingProjectId: PayingProjectId,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 0,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 25_000m,
                    false, false,
                    Rev: 0m)
            ]);

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        entryResult.Value.BaseSalary.Should().Be(500m);

        totalsResult.Value.Should().NotContain(t => t.ProjectId == OriginalProjectId);
        totalsResult.Value.Should().Contain(t => t.ProjectId == PayingProjectId && t.Amount == 1500m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldSplitComplementBucket_60And40()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 1500m,
            complementPayingProjects:
            [
                new ComplementPayingProjectInput(SplitProjectAId, 60m),
                new ComplementPayingProjectInput(SplitProjectBId, 40m)
            ],
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 0,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 25_000m,
                    false, false,
                    Rev: 0m)
            ]);

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == SplitProjectAId && t.Amount == 900m);
        totalsResult.Value.Should().Contain(t => t.ProjectId == SplitProjectBId && t.Amount == 600m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldRouteContingencyFixed_ToLimaKarttos()
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Contingência",
            CalculationType = CalculationProfile.AllocatedFixed,
            IsAllocatedFixed = true,
            RoutesFixedToLimaKarttos = true
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.AllocatedFixed,
                BaseSalary = 5000m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 5000m,
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)],
            RateioProjectEntries = [new RateioProjectEntryInput(OtherId, null)]
        };

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = LimaKarttosId, Amount = 5000m });
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldFail_WhenComplementPayingProjectsDoNotSum100()
    {
        var input = CreateCommercialInput(
            complementPayingProjects:
            [
                new ComplementPayingProjectInput(SplitProjectAId, 60m),
                new ComplementPayingProjectInput(SplitProjectBId, 30m)
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

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsFailure.Should().BeTrue();
        totalsResult.Error!.Code.Should().Be("payroll.complement_paying_projects_invalid_sum");
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldAddManualBonus_ToSpecifiedProject()
    {
        var input = CreateCommercialInput(
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    0, 0, false, false, 0,
                    SalesAmount: 50_000m,
                    false, false,
                    0m)
            ],
            bonuses: [new BonusEntryInput(PayingProjectId, 250m)]);

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == PayingProjectId && t.Amount == 250m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReconcileCommercialTotals_WithEntryTotal()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;
        level.FtdBonusEvery = 0;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 750,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m)
            ],
            deductions: [new DeductionEntryInput(100m)]);

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        entryResult.Value.CommissionAmount.Should().Be(1500m);
        entryResult.Value.BaseSalary.Should().Be(0m);

        var projectSum = totalsResult.Value.Sum(t => t.Amount);
        var unallocatedBonuses = input.BonusEntries
            .Where(b => !b.ProjectId.HasValue)
            .Sum(b => b.Value);
        var deductions = input.DeductionEntries.Sum(d => d.Value);

        projectSum.Should().Be(entryResult.Value.TotalAmount + deductions - unallocatedBonuses);
        totalsResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = OriginalProjectId, Amount = 1500m });
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldExclude3CSportsFromSupervisorFixed()
    {
        var projectOtherId = Guid.NewGuid();
        var level = CreateSupervisorLevel();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
                CalculationType = CalculationProfile.CommercialSupervisor
            },
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 3000m,
            ProjectSnapshots =
            [
                new ProjectCalculationSnapshot(ThreeCSportsId, ExcludesSupervisorFixedAllocation: true),
                new ProjectCalculationSnapshot(projectOtherId)
            ],
            SupervisorProjectEntries =
            [
                new SupervisorProjectEntryInput(ThreeCSportsId, 0, 0, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectOtherId, 0, 0, 0m, 0m, false, false)
            ]
        };

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == projectOtherId && t.Amount == 3000m);
        totalsResult.Value.Should().NotContain(t => t.ProjectId == ThreeCSportsId);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldApplyRoleChangeOnce_WithManualBonus()
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

        var commercialLevel = CreateCommercialLevel();
        commercialLevel.SalesBonusEvery = 0m;
        var fixedLevel = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.FixedBonus,
            BaseSalary = 3100m
        };

        var commercialProjectId = Guid.NewGuid();
        var fixedProjectId = Guid.NewGuid();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = commercialDepartment,
            CareerLevel = commercialLevel,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1500m,
            CommercialProjectEntries =
            [
                new CommercialAnalystProjectEntryInput(
                    commercialProjectId,
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
                    new DateOnly(Year, March, 16),
                    new PayrollRoleSnapshot
                    {
                        Department = fixedDepartment,
                        CareerLevel = fixedLevel,
                        FullBaseSalary = 3100m,
                        ProjectEntries = [new ProjectEntryInput(fixedProjectId, 0m)]
                    })
            ],
            BonusEntries = [new BonusEntryInput(fixedProjectId, 100m)],
            ProjectSnapshots = [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)]
        };

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == commercialProjectId);
        totalsResult.Value.Should().Contain(t => t.ProjectId == fixedProjectId && t.Amount >= 100m);
        totalsResult.Value.Count(t => t.ProjectId == fixedProjectId).Should().Be(1);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldUseManagementBreakdownOnly()
    {
        var managementProjectId = Guid.NewGuid();
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
                    [new ManagementProjectBreakdownInput(managementProjectId, 1750m)])
            ]
        };

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ProjectId = managementProjectId, Amount = 1750m });
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReturnEmpty_ForCommissionOnlyProfile()
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

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().BeEmpty();
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldAllocateExtraFtd_ToComplementBucket()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var level = CreateCommercialLevel();

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    projectA,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 400,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m),
                new CommercialAnalystProjectEntryInput(
                    projectB,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 100,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m)
            ],
            complementPayingProjects: [new ComplementPayingProjectInput(PayingProjectId, 100m)],
            projectSnapshots: [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)]);

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == PayingProjectId && t.Amount >= 350m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldDefaultComplementToLimaKarttos()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 0,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 25_000m,
                    false, false,
                    Rev: 0m)
            ],
            projectSnapshots: [new ProjectCalculationSnapshot(LimaKarttosId, IsDefaultAllocationTarget: true)]);

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == LimaKarttosId && t.Amount == 1500m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldUseNetCommission_WhenRedirectingSmallCommissionWithPlatform()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;

        var input = CreateCommercialInput(
            level: level,
            baseSalary: 80m,
            commissionPayingProjectId: PayingProjectId,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 10,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 1800m,
                    false, false,
                    Rev: 0m)
            ]);

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        entryResult.Value.CommissionAmount.Should().BeLessThan(100m);
        totalsResult.Value.Should().Contain(t => t.ProjectId == PayingProjectId && t.Amount == 20m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldNotAllocateDeductions_ToProjects()
    {
        var level = CreateCommercialLevel();
        level.SalesBonusEvery = 0m;
        level.FtdBonusEvery = 0;

        var input = CreateCommercialInput(
            level: level,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    OriginalProjectId,
                    ProjectPlatform.Lastlink,
                    FtdTotal: 750,
                    FtdSuperbet: 0,
                    false, false, 0,
                    SalesAmount: 0m,
                    false, false,
                    Rev: 0m)
            ],
            deductions: [new DeductionEntryInput(500m)]);

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Sum(t => t.Amount).Should().Be(entryResult.Value.TotalAmount + 500m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReconcileSupervisorTotals_WithEntryTotal()
    {
        var projectId = Guid.NewGuid();
        var level = CreateSupervisorLevel();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
                CalculationType = CalculationProfile.CommercialSupervisor
            },
            CareerLevel = level,
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 3000m,
            SupervisorAnalystRevenue = 300m,
            SupervisorProjectEntries =
            [
                new SupervisorProjectEntryInput(
                    projectId,
                    FtdTotal: 100,
                    FtdSuperbet: 10,
                    SalesAmount: 10_000m,
                    AnalystRev: 1000m,
                    IsProjectFtdGoalReached: true,
                    IsProjectSalesGoalReached: true)
            ]
        };

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Sum(t => t.Amount).Should().Be(entryResult.Value.TotalAmount);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReconcilePaidTrafficTotals_WithEntryTotal()
    {
        var projectId = Guid.NewGuid();
        var rateioProjectId = Guid.NewGuid();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Tráfego",
                CalculationType = CalculationProfile.PaidTraffic
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.PaidTraffic,
                BaseSalary = 2000m,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 2000m,
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    projectId,
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ],
            RateioProjectEntries =
            [
                new RateioProjectEntryInput(projectId, null),
                new RateioProjectEntryInput(rateioProjectId, null)
            ]
        };

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Sum(t => t.Amount).Should().Be(entryResult.Value.TotalAmount);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReconcileProjectLeaderTotals_WithEntryTotal()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Líderes",
                CalculationType = CalculationProfile.ProjectLeader,
                LowRevenueThreshold = 200_000m,
                LowRevenueBonusPct = 0.4m
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.ProjectLeader,
                BaseSalary = 4000m,
                CommissionWithoutGoalPct = 1.5m,
                CommissionWithGoalPct = 1.8m,
                CommissionWithSuperGoalPct = 2.1m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 4000m,
            GoalTier = GoalTier.Goal,
            ProjectEntries =
            [
                new ProjectEntryInput(projectA, 250_000m),
                new ProjectEntryInput(projectB, 250_000m)
            ]
        };

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Sum(t => t.Amount).Should().BeApproximately(
            entryResult.Value.TotalAmount,
            0.01m);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldReconcileTipsterTotals_WithEntryTotal()
    {
        var projectId = Guid.NewGuid();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Tipster",
            CalculationType = CalculationProfile.Tipster,
            GoalBonusPercentage = 10m
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.Tipster,
                BaseSalary = 2000m,
                GroupCommissionPerPercent = 100m,
                GroupCommissionPer20Percent = 500m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 2000m,
            GoalTier = GoalTier.Goal,
            ProjectEntries = [new ProjectEntryInput(projectId, 5000m, 40m)],
            RateioProjectEntries = [new RateioProjectEntryInput(projectId, null)]
        };

        var entryResult = _calculator.CalcEntry(input);
        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        entryResult.IsSuccess.Should().BeTrue();
        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Sum(t => t.Amount).Should().Be(entryResult.Value.TotalAmount);
    }

    [Fact]
    public void CalcProjectTotalsForEntry_ShouldAllocateFeiraFixedOnly_WhenGoalReached()
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Administrativo",
            CalculationType = CalculationProfile.AllocatedFixed,
            IsAllocatedFixed = true,
            GoalBonusPercentage = 10m
        };

        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.AllocatedFixed,
                BaseSalary = 3000m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 3000m,
            GoalTier = GoalTier.Goal,
            ProjectEntries =
            [
                new ProjectEntryInput(FeiraId, 0m),
                new ProjectEntryInput(OtherId, 0m)
            ],
            ProjectSnapshots =
            [
                new ProjectCalculationSnapshot(FeiraId, ExcludesGoalBonus: true),
                new ProjectCalculationSnapshot(OtherId)
            ]
        };

        var totalsResult = _calculator.CalcProjectTotalsForEntry(input);

        totalsResult.IsSuccess.Should().BeTrue();
        totalsResult.Value.Should().Contain(t => t.ProjectId == FeiraId && t.Amount == 1500m);
        totalsResult.Value.Should().Contain(t => t.ProjectId == OtherId && t.Amount == 1650m);
    }

    private static PayrollEntryInput CreateCommercialInput(
        CareerLevel? level = null,
        decimal? baseSalary = null,
        Guid? commissionPayingProjectId = null,
        IReadOnlyList<ComplementPayingProjectInput>? complementPayingProjects = null,
        IReadOnlyList<CommercialAnalystProjectEntryInput>? projectEntries = null,
        IReadOnlyList<BonusEntryInput>? bonuses = null,
        IReadOnlyList<DeductionEntryInput>? deductions = null,
        IReadOnlyList<ProjectCalculationSnapshot>? projectSnapshots = null) =>
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
            FullBaseSalary = baseSalary,
            CommissionPayingProjectId = commissionPayingProjectId,
            ComplementPayingProjects = complementPayingProjects ?? [],
            CommercialProjectEntries = projectEntries ?? [],
            BonusEntries = bonuses ?? [],
            DeductionEntries = deductions ?? [],
            ProjectSnapshots = projectSnapshots ?? []
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

    private static CareerLevel CreateSupervisorLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor",
            Profile = CalculationProfile.CommercialSupervisor,
            BaseSalary = 3000m,
            SupFtdSuperbetNoGoal = 4m,
            SupFtdSuperbetWithGoal = 5m,
            SupFtdOtherNoGoal = 0.3m,
            SupFtdOtherWithGoal = 0.5m,
            SupSalesPctNoGoal = 0.5m,
            SupSalesPctWithGoal = 0.8m,
            SupRevPct = 10m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}
