using Core.Auth;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using Core.Domain.TrafficInvestmentCalculation;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Seed;

/// <summary>Opt-in, idempotent Development scenarios for practical browser E2E testing.</summary>
public sealed class DevelopmentDemoDataSeeder
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly SeedOptions _options;
    private readonly DevelopmentFixtureSeeder _fixtures;

    public DevelopmentDemoDataSeeder(
        AppDbContext db,
        UserManager<AppUser> users,
        IOptions<SeedOptions> options,
        DevelopmentFixtureSeeder fixtures)
    {
        _db = db;
        _users = users;
        _options = options.Value;
        _fixtures = fixtures;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RoleUsersPassword))
            throw new InvalidOperationException("Seed:RoleUsersPassword (or DEV_ROLE_USERS_PASSWORD) is required when Seed:LoadDemoData is enabled.");

        await _fixtures.SeedAsync(ct);
        var departmentId = await IdAsync(SeedKeys.Departments.CommercialAnalysts, ct);
        var collaboratorId = await IdAsync(SeedKeys.Collaborators.CommercialAnalystActive, ct);
        var projectId = await IdAsync(SeedKeys.Projects.LastlinkSample, ct);

        var roleUsers = new Dictionary<string, AppUser>();
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Director, AppRoles.Financial, AppRoles.Manager, AppRoles.User })
            roleUsers[role] = await UpsertUserAsync(role, ct);

        var manager = roleUsers[AppRoles.Manager];
        if (!await _db.UserDepartments.AnyAsync(x => x.UserId == manager.Id && x.DepartmentId == departmentId, ct))
            _db.UserDepartments.Add(new UserDepartment { UserId = manager.Id, DepartmentId = departmentId });

        var paymentMethodId = await SeedIdAsync(SeedKeys.Demo.PaymentMethod, ct);
        var payment = await _db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == paymentMethodId, ct);
        if (payment is null) { payment = new PaymentMethod { Id = paymentMethodId }; _db.PaymentMethods.Add(payment); }
        payment.Name = "PIX Demo"; payment.IsActive = true;

        await _db.SaveChangesAsync(ct);
        _db.ChangeTracker.Clear();
        await UpsertOperationalDataAsync(departmentId, collaboratorId, projectId, paymentMethodId, manager.Id, ct);
        await _db.SaveChangesAsync(ct);
        _db.ChangeTracker.Clear();
    }

    private async Task<AppUser> UpsertUserAsync(string role, CancellationToken ct)
    {
        var email = $"{role.ToLowerInvariant()}@corepay.local";
        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = email,
                DisplayName = $"{role} Demo",
                EmailConfirmed = true
            };
            var created = await _users.CreateAsync(user, _options.RoleUsersPassword);
            Ensure(created, $"create demo user {role}");
        }

        if (!await _users.IsInRoleAsync(user, role))
            Ensure(await _users.AddToRoleAsync(user, role), $"assign role {role}");
        return user;
    }

    private async Task UpsertOperationalDataAsync(
        Guid departmentId, Guid collaboratorId, Guid projectId, Guid paymentMethodId, string managerId, CancellationToken ct)
    {
        var statuses = new[]
        {
            PayrollStatus.Draft, PayrollStatus.PendingApproval, PayrollStatus.Approved,
            PayrollStatus.Paid, PayrollStatus.Rejected
        };
        for (var index = 0; index < statuses.Length; index++)
        {
            var status = statuses[index];
            var key = status.ToString().ToLowerInvariant();
            var payrollId = await SeedIdAsync(SeedKeys.Demo.Payroll(key), ct);
            if (!await _db.Payrolls.AsNoTracking().AnyAsync(x => x.Id == payrollId, ct))
            {
                var payroll = new Payroll
                {
                    Id = payrollId, DepartmentId = departmentId, Month = index + 1, Year = 2026,
                    Status = status, TotalAmount = 4250m,
                    SubmittedBy = status == PayrollStatus.Draft ? null : "Manager Demo",
                    SubmittedByUserId = status == PayrollStatus.Draft ? null : managerId,
                    RejectionComment = status == PayrollStatus.Rejected ? "Ajustar dados da demonstração" : null,
                    ApprovedBy = status is PayrollStatus.Approved or PayrollStatus.Paid ? "Director Demo" : null,
                    ApprovedAt = status is PayrollStatus.Approved or PayrollStatus.Paid ? new DateTimeOffset(2026, index + 1, 20, 12, 0, 0, TimeSpan.FromHours(-3)) : null
                };
                payroll.Entries.Add(new PayrollCollaboratorEntry
                {
                    Id = await SeedIdAsync(SeedKeys.Demo.PayrollEntry(key), ct),
                    CollaboratorId = collaboratorId,
                    CollaboratorName = "Colaborador Demo Ativo",
                    PixKey = "pix-demo-ativo@corepay.test",
                    DepartmentId = departmentId,
                    CalculationProfile = CalculationProfile.CommercialAnalyst,
                    FullBaseSalary = 3500m,
                    IsApproved = status is PayrollStatus.Approved or PayrollStatus.Paid,
                    IsPaid = status == PayrollStatus.Paid,
                    NfSent = status == PayrollStatus.Paid,
                    Payload = new PayrollCollaboratorEntryPayload
                    {
                        CalculatedResult = new PayrollEntryResult
                        {
                            TotalAmount = 4250m,
                            BaseSalary = 3500m,
                            CommissionAmount = 750m
                        }
                    }
                });
                _db.Payrolls.Add(payroll);
            }

            if (status != PayrollStatus.Draft)
            {
                var notificationId = await SeedIdAsync(SeedKeys.Demo.Notification(key), ct);
                if (!await _db.Notifications.AnyAsync(x => x.Id == notificationId, ct))
                    _db.Notifications.Add(new Notification
                    {
                        Id = notificationId, PayrollId = payrollId, Type = $"payroll_{key}",
                        Title = $"Folha {status}", Message = "Cenário E2E de desenvolvimento",
                        RoleTarget = AppRoles.Admin, CreatedAt = new DateTimeOffset(2026, index + 1, 21, 9, 0, 0, TimeSpan.FromHours(-3))
                    });
            }

            await _db.SaveChangesAsync(ct);
        }

        await UpsertRevenueMetricTrafficCashflowAsync(departmentId, collaboratorId, projectId, paymentMethodId, ct);
    }

    private async Task UpsertRevenueMetricTrafficCashflowAsync(
        Guid departmentId, Guid collaboratorId, Guid projectId, Guid paymentMethodId, CancellationToken ct)
    {
        var revenueId = await SeedIdAsync(SeedKeys.Demo.Revenue, ct);
        if (!await _db.ProjectRevenues.AnyAsync(x => x.Id == revenueId, ct))
            _db.ProjectRevenues.Add(new ProjectRevenue { Id = revenueId, ProjectId = projectId, Month = 8, Year = 2026, ValueIgaming = 120000m, ValueVendas = 80000m, Value = 200000m, GroupPercentage = 18m, Notes = "Demo E2E" });

        var metricId = await SeedIdAsync(SeedKeys.Demo.AnalystMetric, ct);
        if (!await _db.AnalystMetrics.AnyAsync(x => x.Id == metricId, ct))
            _db.AnalystMetrics.Add(new AnalystMetric { Id = metricId, CollaboratorId = collaboratorId, ProjectId = projectId, Month = 8, Year = 2026, FtdTotal = 420, CpaCount = 37 });

        var investmentId = await SeedIdAsync(SeedKeys.Demo.TrafficInvestment, ct);
        if (!await _db.TrafficInvestments.AnyAsync(x => x.Id == investmentId, ct))
            _db.TrafficInvestments.Add(new TrafficInvestment
            {
                Id = investmentId, ProjectId = projectId, Month = 8, Year = 2026, MonthlyTarget = 40000m,
                Weeks =
                [
                    new TrafficInvestmentWeek
                    {
                        Id = Guid.NewGuid(), WeekNumber = 1,
                        Deposits = [new TrafficWeekDeposit { Id = Guid.NewGuid(), RequestedAmount = 10000m, DepositedAmount = 10000m, Status = TrafficDepositStatus.Deposited }],
                        ChannelSpends = [new TrafficWeekChannelSpend { Id = Guid.NewGuid(), Channel = TrafficMediaChannel.Instagram, Amount = 7200m }]
                    }
                ]
            });

        var depositId = await SeedIdAsync(SeedKeys.Demo.TrafficDeposit, ct);
        if (!await _db.TrafficProjectDeposits.AnyAsync(x => x.Id == depositId, ct))
            _db.TrafficProjectDeposits.Add(new TrafficProjectDeposit { Id = depositId, ProjectId = projectId, DepositDate = new(2026, 8, 5), Amount = 10000m, Notes = "Aporte demo" });

        await UpsertCostAsync(SeedKeys.Demo.CashflowIncome, ProjectCostType.Entrada, ProjectCostCategory.Plataforma, 25000m);
        await UpsertCostAsync(SeedKeys.Demo.CashflowExpense, ProjectCostType.Saida, ProjectCostCategory.Trafego, 8500m);

        async Task UpsertCostAsync(string key, ProjectCostType type, ProjectCostCategory category, decimal amount)
        {
            var id = await SeedIdAsync(key, ct);
            if (!await _db.ProjectCosts.AnyAsync(x => x.Id == id, ct))
                _db.ProjectCosts.Add(new ProjectCost
                {
                    Id = id, Type = type, Category = category, Amount = amount, TransactionDate = new(2026, 8, 10),
                    Month = 8, Year = 2026, ProjectId = projectId, DepartmentId = departmentId,
                    PaymentMethodId = paymentMethodId, Requester = "Demo E2E", Notes = "Registro de desenvolvimento"
                });
        }
    }

    private async Task<Guid> IdAsync(string key, CancellationToken ct) =>
        (await _db.SeedEntities.SingleAsync(x => x.Key == key, ct)).EntityId;

    private async Task<Guid> SeedIdAsync(string key, CancellationToken ct)
    {
        var mapping = await _db.SeedEntities.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (mapping is not null) return mapping.EntityId;
        mapping = new SeedEntity { Key = key, EntityId = Guid.NewGuid() };
        _db.SeedEntities.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping.EntityId;
    }

    private static void Ensure(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to {operation}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
    }
}
