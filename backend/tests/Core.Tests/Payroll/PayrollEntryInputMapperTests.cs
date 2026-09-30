using Core.Application.Payrolls;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using BuildingBlocks.Results;
using FluentAssertions;
using DomainPayroll = Core.Domain.Payroll;

namespace Core.Tests.Payrolls;

public class PayrollEntryInputMapperTests
{
    private static readonly Guid ProjectA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProjectB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid DepartmentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid LevelId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Map_ShouldEnrichCommercialPlatform_FromProjectNotClient()
    {
        var payroll = CreatePayroll();
        var entry = CreateEntry(CalculationProfile.CommercialAnalyst);
        var collaborator = CreateCollaborator();
        var department = CreateDepartment();
        var level = CreateLevel();
        var projects = new List<Project>
        {
            new()
            {
                Id = ProjectA,
                Name = "Hubla Demo",
                Platform = ProjectPlatform.Hubla,
                IsActive = true
            }
        };

        var request = new PreviewPayrollEntryRequest(
            CommercialProjectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    ProjectA,
                    ProjectPlatform.Lastlink,
                    0, 0, false, false, 0,
                    10_000m, false, false, 0m)
            ]);

        var result = PayrollEntryInputMapper.Map(
            payroll,
            entry,
            collaborator,
            department,
            level,
            null,
            projects.ToDictionary(project => project.Id),
            new Dictionary<Guid, Department> { [DepartmentId] = department },
            new Dictionary<Guid, CareerLevel> { [LevelId] = level },
            request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CommercialProjectEntries[0].Platform.Should().Be(ProjectPlatform.Hubla);
    }

    [Fact]
    public void Map_ShouldReturnNotFound_WhenCommercialProjectMissing()
    {
        var payroll = CreatePayroll();
        var entry = CreateEntry(CalculationProfile.CommercialAnalyst);
        var collaborator = CreateCollaborator();
        var department = CreateDepartment();
        var level = CreateLevel();

        var request = new PreviewPayrollEntryRequest(
            CommercialProjectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    0, 0, false, false, 0,
                    100m, false, false, 0m)
            ]);

        var result = PayrollEntryInputMapper.Map(
            payroll,
            entry,
            collaborator,
            department,
            level,
            null,
            new Dictionary<Guid, Project>(),
            new Dictionary<Guid, Department>(),
            new Dictionary<Guid, CareerLevel>(),
            request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.project_not_found");
    }

    [Fact]
    public void ApplyRequest_ShouldPersistEditableFieldsOnEntry()
    {
        var entry = CreateEntry(CalculationProfile.CommercialAnalyst);
        var request = new PreviewPayrollEntryRequest(
            GoalTier: GoalTier.Goal,
            FinalSalary: 4200m,
            BetanoInternaCount: 2,
            BetanoMundoBetCount: 1,
            SupervisorAnalystRevenue: 150m,
            CommissionPayingProjectId: ProjectA,
            CommercialProjectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    ProjectA,
                    ProjectPlatform.Lastlink,
                    10, 0, false, false, 0,
                    5000m, false, false, 0m)
            ],
            BonusEntries: [new BonusEntryInput(ProjectA, 300m, "Bônus")],
            DeductionEntries: [new DeductionEntryInput(100m, "Desconto")]);

        PayrollEntryInputMapper.ApplyRequest(entry, request);

        entry.GoalTier.Should().Be(GoalTier.Goal);
        entry.FinalSalary.Should().Be(4200m);
        entry.BetanoInternaCount.Should().Be(2);
        entry.Payload.CommercialProjectEntries.Should().ContainSingle();
        entry.Payload.BonusEntries.Should().ContainSingle();
        entry.Payload.DeductionEntries.Should().ContainSingle();

        var roundTrip = PayrollEntryInputMapper.ToPreviewRequest(entry);
        roundTrip.GoalTier.Should().Be(GoalTier.Goal);
        roundTrip.CommercialProjectEntries.Should().ContainSingle();
        roundTrip.BonusEntries.Should().ContainSingle();
    }

    [Fact]
    public void Map_ShouldResolveRoleChangeEntities_FromTrustedIds()
    {
        var payroll = CreatePayroll();
        var entry = CreateEntry(CalculationProfile.FixedBonus);
        var collaborator = CreateCollaborator();
        var department = CreateDepartment();
        var level = CreateLevel();
        var otherDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Outro",
            CalculationType = CalculationProfile.CommissionOnly
        };

        var request = new PreviewPayrollEntryRequest(
            RoleChanges:
            [
                new PayrollRoleChangeEntry
                {
                    ChangeDate = new DateOnly(2031, 6, 16),
                    Role = new PayrollRoleChangeSnapshot
                    {
                        DepartmentId = otherDepartment.Id,
                        CareerLevelId = LevelId,
                        FullBaseSalary = 3000m
                    }
                }
            ]);

        var result = PayrollEntryInputMapper.Map(
            payroll,
            entry,
            collaborator,
            department,
            level,
            null,
            new Dictionary<Guid, Project>(),
            new Dictionary<Guid, Department>
            {
                [DepartmentId] = department,
                [otherDepartment.Id] = otherDepartment
            },
            new Dictionary<Guid, CareerLevel> { [LevelId] = level },
            request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RoleChanges.Should().ContainSingle();
        result.Value.RoleChanges[0].Role.Department!.Id.Should().Be(otherDepartment.Id);
        result.Value.RoleChanges[0].Role.CareerLevel!.Id.Should().Be(LevelId);
    }

    public static IEnumerable<object[]> MissingProjectReferences =>
    [
        ["projectEntries"],
        ["rateioProjectEntries"],
        ["trafficProjectEntries"],
        ["supervisorProjectEntries"],
        ["managementBreakdown"],
        ["bonusEntries"],
        ["complementPayingProjects"]
    ];

    [Theory]
    [MemberData(nameof(MissingProjectReferences))]
    public void Map_ShouldRejectMissingProject_InEveryProjectCollection(string collection)
    {
        var missingId = Guid.NewGuid();
        var request = collection switch
        {
            "projectEntries" => new PreviewPayrollEntryRequest(
                ProjectEntries: [new ProjectEntryInput(missingId, 1m)]),
            "rateioProjectEntries" => new PreviewPayrollEntryRequest(
                RateioProjectEntries: [new RateioProjectEntryInput(missingId)]),
            "trafficProjectEntries" => new PreviewPayrollEntryRequest(
                TrafficProjectEntries: [new TrafficProjectEntryInput(missingId, 1m)]),
            "supervisorProjectEntries" => new PreviewPayrollEntryRequest(
                SupervisorProjectEntries:
                [
                    new SupervisorProjectEntryInput(missingId, 0, 0, 0m, 0m, false, false)
                ]),
            "managementBreakdown" => new PreviewPayrollEntryRequest(
                ManagementRevenueEntries:
                [
                    new ManagementRevenueEntryInput(
                        1m,
                        [new ManagementProjectBreakdownInput(missingId, 1m)])
                ]),
            "bonusEntries" => new PreviewPayrollEntryRequest(
                BonusEntries: [new BonusEntryInput(missingId, 1m)]),
            "complementPayingProjects" => new PreviewPayrollEntryRequest(
                ComplementPayingProjects: [new ComplementPayingProjectInput(missingId, 100m)]),
            _ => throw new ArgumentOutOfRangeException(nameof(collection))
        };

        var result = Map(request, new Dictionary<Guid, Project>());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.project_not_found");
        result.Error.Message.Should().Be("Project not found or inactive.");
    }

    [Fact]
    public void Map_ShouldRejectMissingCommissionPayingProject()
    {
        var result = Map(
            new PreviewPayrollEntryRequest(CommissionPayingProjectId: Guid.NewGuid()),
            new Dictionary<Guid, Project>());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.project_not_found");
    }

    [Fact]
    public void Map_ShouldRejectMissingProjectInsideRoleChange()
    {
        var request = new PreviewPayrollEntryRequest(
            RoleChanges:
            [
                new PayrollRoleChangeEntry
                {
                    ChangeDate = new DateOnly(2031, 6, 16),
                    Role = new PayrollRoleChangeSnapshot
                    {
                        ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 1m)]
                    }
                }
            ]);

        var result = Map(request, new Dictionary<Guid, Project>());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.project_not_found");
    }

    [Fact]
    public void Map_ShouldRejectProjectExcludedFromActiveProjectSet()
    {
        var inactiveProject = new Project
        {
            Id = ProjectA,
            Name = "Inactive",
            IsActive = false
        };

        var result = Map(
            new PreviewPayrollEntryRequest(ProjectEntries: [new ProjectEntryInput(inactiveProject.Id, 1m)]),
            new Dictionary<Guid, Project>());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.project_not_found");
    }

    [Fact]
    public void Map_ShouldAcceptValidProjectReferences()
    {
        var project = new Project
        {
            Id = ProjectA,
            Name = "Active",
            IsActive = true
        };
        var request = new PreviewPayrollEntryRequest(
            CommissionPayingProjectId: ProjectA,
            ProjectEntries: [new ProjectEntryInput(ProjectA, 1m)],
            BonusEntries: [new BonusEntryInput(null, 1m)]);

        var result = Map(request, new Dictionary<Guid, Project> { [ProjectA] = project });

        result.IsSuccess.Should().BeTrue();
    }

    private static Result<PayrollEntryInput> Map(
        PreviewPayrollEntryRequest request,
        IReadOnlyDictionary<Guid, Project> projects) =>
        PayrollEntryInputMapper.Map(
            CreatePayroll(),
            CreateEntry(CalculationProfile.FixedBonus),
            CreateCollaborator(),
            CreateDepartment(),
            CreateLevel(),
            null,
            projects,
            new Dictionary<Guid, Department> { [DepartmentId] = CreateDepartment() },
            new Dictionary<Guid, CareerLevel> { [LevelId] = CreateLevel() },
            request);

    private static DomainPayroll CreatePayroll() =>
        new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = DepartmentId,
            Month = 6,
            Year = 2031,
            Status = PayrollStatus.Draft
        };

    private static PayrollCollaboratorEntry CreateEntry(CalculationProfile profile) =>
        new()
        {
            Id = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            CollaboratorName = "Test",
            CalculationProfile = profile,
            DepartmentId = DepartmentId,
            CareerLevelId = LevelId,
            FullBaseSalary = 3500m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            DepartmentId = DepartmentId,
            CareerLevelId = LevelId,
            AdmissionDate = new DateOnly(2024, 1, 1)
        };

    private static Department CreateDepartment() =>
        new()
        {
            Id = DepartmentId,
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = LevelId,
            Profile = CalculationProfile.CommercialAnalyst,
            BaseSalary = 3500m,
            SalesPctBase = 4m,
            FtdRateBase = 2m
        };
}
