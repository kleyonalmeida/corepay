using Core.Application.Payrolls;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.Seed;

namespace WebAPI.Tests.Payroll;

[Collection("WebApiIntegration")]
public class PayrollPersistenceTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollPersistenceTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveAndGet_ShouldRoundTripFullCollaboratorEntryWithoutFieldLoss()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var departmentId = seedEntities.Single(s => s.Key == SeedKeys.Departments.CommercialAnalysts).EntityId;
        var collaboratorId = seedEntities.Single(s => s.Key == SeedKeys.Collaborators.CommercialAnalystActive).EntityId;
        var levelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialAnalystJunior).EntityId;
        var lastlinkProjectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var hublaProjectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.HublaSample).EntityId;
        var limaKarttosId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LimaKarttos).EntityId;
        var managementDeptId = seedEntities.Single(s => s.Key == SeedKeys.Departments.Management).EntityId;
        var managementLevelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialSupervisor).EntityId;

        var payrollId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var original = new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = departmentId,
            Month = 3,
            Year = 2026,
            Status = PayrollStatus.Draft,
            TotalAmount = 0m,
            Entries =
            [
                new PayrollCollaboratorEntry
                {
                    Id = entryId,
                    PayrollId = payrollId,
                    CollaboratorId = collaboratorId,
                    CollaboratorName = DevelopmentFixtureData.ActiveCollaboratorName,
                    PixKey = "ana@pix.test",
                    AdmissionDate = new DateOnly(2024, 1, 15),
                    CareerLevelName = "Analista Júnior",
                    CalculationProfile = CalculationProfile.CommercialAnalyst,
                    DepartmentId = departmentId,
                    CareerLevelId = levelId,
                    FullBaseSalary = 3500m,
                    GoalTier = GoalTier.Goal,
                    FinalSalary = 4200m,
                    BetanoInternaCount = 5,
                    BetanoMundoBetCount = 2,
                    SupervisorAnalystRevenue = 1500m,
                    CommissionPayingProjectId = lastlinkProjectId,
                    TrafficSeniorLevelId = null,
                    IsApproved = true,
                    IsPaid = false,
                    NfSent = true,
                    Payload = new PayrollCollaboratorEntryPayload
                    {
                        ProjectEntries =
                        [
                            new ProjectEntryInput(lastlinkProjectId, 80_000m, 0m),
                            new ProjectEntryInput(hublaProjectId, 45_000m, 12m)
                        ],
                        CommercialProjectEntries =
                        [
                            new CommercialAnalystProjectEntryInput(
                                lastlinkProjectId,
                                ProjectPlatform.Lastlink,
                                FtdTotal: 120,
                                FtdSuperbet: 30,
                                IsFtdGoalReached: true,
                                IsProjectFtdGoalReached: false,
                                CpaCount: 4,
                                SalesAmount: 25_000m,
                                IsSalesGoalReached: true,
                                IsProjectSalesGoalReached: false,
                                Rev: 800m),
                            new CommercialAnalystProjectEntryInput(
                                hublaProjectId,
                                ProjectPlatform.Hubla,
                                FtdTotal: 80,
                                FtdSuperbet: 10,
                                IsFtdGoalReached: false,
                                IsProjectFtdGoalReached: true,
                                CpaCount: 2,
                                SalesAmount: 12_000m,
                                IsSalesGoalReached: false,
                                IsProjectSalesGoalReached: true,
                                Rev: 400m)
                        ],
                        BonusEntries =
                        [
                            new BonusEntryInput(lastlinkProjectId, 500m, "Bônus campanha")
                        ],
                        DeductionEntries =
                        [
                            new DeductionEntryInput(200m, "Adiantamento")
                        ],
                        ComplementPayingProjects =
                        [
                            new ComplementPayingProjectInput(lastlinkProjectId, 60m),
                            new ComplementPayingProjectInput(hublaProjectId, 40m)
                        ],
                        ProjectSnapshots =
                        [
                            new ProjectCalculationSnapshot(limaKarttosId, IsDefaultAllocationTarget: true)
                        ],
                        RoleChanges =
                        [
                            new PayrollRoleChangeEntry
                            {
                                ChangeDate = new DateOnly(2026, 3, 10),
                                Role = new PayrollRoleChangeSnapshot
                                {
                                    DepartmentId = managementDeptId,
                                    CareerLevelId = managementLevelId,
                                    FullBaseSalary = 5000m,
                                    GoalTier = GoalTier.SuperGoal,
                                    FinalSalary = null,
                                    BetanoInternaCount = 1,
                                    BetanoMundoBetCount = 0,
                                    CommercialProjectEntries =
                                    [
                                        new CommercialAnalystProjectEntryInput(
                                            lastlinkProjectId,
                                            ProjectPlatform.Lastlink,
                                            FtdTotal: 50,
                                            FtdSuperbet: 5,
                                            IsFtdGoalReached: true,
                                            IsProjectFtdGoalReached: true,
                                            CpaCount: 1,
                                            SalesAmount: 5000m,
                                            IsSalesGoalReached: true,
                                            IsProjectSalesGoalReached: true,
                                            Rev: 100m)
                                    ],
                                    ComplementPayingProjects =
                                    [
                                        new ComplementPayingProjectInput(lastlinkProjectId, 100m)
                                    ]
                                }
                            }
                        ]
                    }
                }
            ]
        };

        var saveResult = await store.SaveAsync(original);
        saveResult.IsSuccess.Should().BeTrue(saveResult.Error?.Message);

        using var readScope = _factory.Services.CreateScope();
        var readStore = readScope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var getResult = await readStore.GetByIdAsync(payrollId);
        getResult.IsSuccess.Should().BeTrue();
        var loaded = getResult.Value!;

        loaded.Should().BeEquivalentTo(original, options => options
            .Excluding(p => p.Department)
            .Excluding(p => p.Entries));

        loaded.Entries.Should().HaveCount(1);
        loaded.Entries.Single().Should().BeEquivalentTo(original.Entries.Single(), options => options
            .Excluding(e => e.Payroll));
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectDuplicateCompetenceForSameDepartment()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var departmentId = seedEntities.Single(s => s.Key == SeedKeys.Departments.CommercialAnalysts).EntityId;

        var first = new Core.Domain.Payroll
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Month = 7,
            Year = 2026,
            Status = PayrollStatus.Draft
        };

        var duplicate = new Core.Domain.Payroll
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Month = 7,
            Year = 2026,
            Status = PayrollStatus.Draft
        };

        (await store.SaveAsync(first)).IsSuccess.Should().BeTrue();
        var duplicateResult = await store.SaveAsync(duplicate);

        duplicateResult.IsFailure.Should().BeTrue();
        duplicateResult.Error!.Code.Should().Be("payrolls.competence_duplicate");
    }
}
