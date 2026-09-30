using Core.Application.Payrolls;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using WebAPI.Tests.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Tests.Payroll;

internal static class PayrollTestHelper
{
    public static async Task<Core.Domain.Payroll> CreatePaidPayrollAsync(
        IServiceProvider services,
        string departmentSeedKey,
        string collaboratorSeedKey,
        int month,
        int year,
        decimal totalAmount = 12_500m)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var departmentId = seedEntities.Single(s => s.Key == departmentSeedKey).EntityId;
        var collaboratorId = seedEntities.Single(s => s.Key == collaboratorSeedKey).EntityId;
        var levelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialAnalystJunior).EntityId;
        var projectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;

        var payrollId = Guid.NewGuid();
        var payroll = new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = departmentId,
            Month = month,
            Year = year,
            Status = PayrollStatus.Paid,
            TotalAmount = totalAmount,
            SubmittedBy = "manager@test",
            ApprovedBy = "director@test",
            ApprovedAt = DateTimeOffset.UtcNow,
            Entries =
            [
                new PayrollCollaboratorEntry
                {
                    Id = Guid.NewGuid(),
                    PayrollId = payrollId,
                    CollaboratorId = collaboratorId,
                    CollaboratorName = DevelopmentFixtureData.ActiveCollaboratorName,
                    PixKey = DevelopmentFixtureData.ActiveCollaboratorPixKey,
                    AdmissionDate = new DateOnly(2024, 1, 15),
                    CareerLevelName = "Analista Júnior",
                    CalculationProfile = CalculationProfile.CommercialAnalyst,
                    DepartmentId = departmentId,
                    CareerLevelId = levelId,
                    FullBaseSalary = 3500m,
                    GoalTier = GoalTier.Goal,
                    FinalSalary = 4200m,
                    IsApproved = true,
                    IsPaid = true,
                    NfSent = true,
                    Payload = new PayrollCollaboratorEntryPayload
                    {
                        ProjectEntries = [new ProjectEntryInput(projectId, 80_000m, 0m)],
                        BonusEntries = [new BonusEntryInput(projectId, 500m, "Bônus campanha")],
                        DeductionEntries = [new DeductionEntryInput(200m, "Adiantamento")],
                        CalculatedResult = new PayrollEntryResult
                        {
                            TotalAmount = totalAmount,
                            BaseSalary = 0m,
                            CommissionAmount = totalAmount,
                            GoalBonusAmount = 0m,
                            GroupCommissionAmount = 0m,
                            PlatformTotal = 0m
                        },
                        DisplayProjectTotals =
                        [
                            new ProjectTotalAllocation(projectId, totalAmount)
                        ]
                    }
                }
            ]
        };

        var result = await store.SaveAsync(payroll);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.Message);
        }

        return result.Value!;
    }

    public static async Task<Core.Domain.Payroll> CreateDraftPayrollAsync(
        IServiceProvider services,
        string departmentSeedKey,
        int month,
        int year,
        params string[] collaboratorSeedKeys)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var collaboratorIds = collaboratorSeedKeys
            .Select(key => seedEntities.Single(s => s.Key == key).EntityId)
            .ToList();

        return await CreateDraftPayrollWithCollaboratorIdsAsync(
            services,
            departmentSeedKey,
            month,
            year,
            collaboratorIds.ToArray());
    }

    public static async Task<Core.Domain.Payroll> CreateDraftPayrollWithCollaboratorIdsAsync(
        IServiceProvider services,
        string departmentSeedKey,
        int month,
        int year,
        params Guid[] collaboratorIds)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var departmentId = seedEntities.Single(s => s.Key == departmentSeedKey).EntityId;

        var result = await store.CreatePayrollAsync(
            new CreatePayrollRequest(departmentId, month, year, collaboratorIds),
            new PayrollAccessContext("test", ["Admin"], null));

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.Message);
        }

        var payrollResult = await store.GetByIdAsync(result.Value!.Id);
        return payrollResult.Value!;
    }

    public static async Task<Guid> CreateActiveCollaboratorAsync(
        IServiceProvider services,
        string departmentSeedKey,
        string levelSeedKey,
        string name,
        string pixKey)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var departmentId = seedEntities.Single(s => s.Key == departmentSeedKey).EntityId;
        var levelId = seedEntities.Single(s => s.Key == levelSeedKey).EntityId;

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = name,
            DepartmentId = departmentId,
            CareerLevelId = levelId,
            PixKey = pixKey,
            AdmissionDate = new DateOnly(2024, 6, 1),
            BaseSalary = 3200m,
            IsActive = true
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();
        return collaborator.Id;
    }

    public static async Task<Core.Domain.Payroll> CreatePendingApprovalPayrollAsync(
        IServiceProvider services,
        string departmentSeedKey,
        string collaboratorSeedKey,
        int month,
        int year)
    {
        var draft = await CreateDraftPayrollAsync(
            services,
            departmentSeedKey,
            month,
            year,
            collaboratorSeedKey);

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var payroll = await dbContext.Payrolls.SingleAsync(p => p.Id == draft.Id);
        payroll.Status = PayrollStatus.PendingApproval;
        payroll.SubmittedBy = "manager@test";
        await dbContext.SaveChangesAsync();
        return payroll;
    }

    public static async Task<Core.Domain.Payroll> CreateRejectedPayrollWithApprovedEntryAsync(
        IServiceProvider services,
        string departmentSeedKey,
        string approvedCollaboratorSeedKey,
        Guid secondCollaboratorId,
        int month,
        int year)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPayrollStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();

        var departmentId = seedEntities.Single(s => s.Key == departmentSeedKey).EntityId;
        var approvedCollaboratorId = seedEntities.Single(s => s.Key == approvedCollaboratorSeedKey).EntityId;

        var createResult = await store.CreatePayrollAsync(
            new CreatePayrollRequest(
                departmentId,
                month,
                year,
                [approvedCollaboratorId, secondCollaboratorId]),
            new PayrollAccessContext("test", ["Admin"], null));

        if (createResult.IsFailure)
        {
            throw new InvalidOperationException(createResult.Error!.Message);
        }

        var payroll = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == createResult.Value!.Id);

        payroll.Status = PayrollStatus.Rejected;
        payroll.RejectionComment = "Ajustar valores";
        payroll.Entries.First(e => e.CollaboratorId == approvedCollaboratorId).IsApproved = true;

        await dbContext.SaveChangesAsync();
        return payroll;
    }
}
