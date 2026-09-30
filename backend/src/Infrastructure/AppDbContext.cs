using System.Text.Json;
using Core.Domain;
using Core.Domain.TrafficInvestmentCalculation;
using Infrastructure.Identity;
using Infrastructure.Payrolls;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<CareerLevel> CareerLevels => Set<CareerLevel>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectRevenue> ProjectRevenues => Set<ProjectRevenue>();

    public DbSet<AnalystMetric> AnalystMetrics => Set<AnalystMetric>();

    public DbSet<TrafficInvestment> TrafficInvestments => Set<TrafficInvestment>();

    public DbSet<TrafficInvestmentWeek> TrafficInvestmentWeeks => Set<TrafficInvestmentWeek>();

    public DbSet<TrafficWeekDeposit> TrafficWeekDeposits => Set<TrafficWeekDeposit>();

    public DbSet<TrafficWeekChannelSpend> TrafficWeekChannelSpends => Set<TrafficWeekChannelSpend>();

    public DbSet<TrafficProjectDeposit> TrafficProjectDeposits => Set<TrafficProjectDeposit>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<ProjectCost> ProjectCosts => Set<ProjectCost>();

    public DbSet<Collaborator> Collaborators => Set<Collaborator>();

    public DbSet<Payroll> Payrolls => Set<Payroll>();

    public DbSet<PayrollCollaboratorEntry> PayrollCollaboratorEntries => Set<PayrollCollaboratorEntry>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationReadReceipt> NotificationReadReceipts => Set<NotificationReadReceipt>();

    public DbSet<SeedEntity> SeedEntities => Set<SeedEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Key).IsUnique();
            entity.Property(p => p.Key).HasMaxLength(128);
            entity.Property(p => p.Description).HasMaxLength(512);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserDepartment>(entity =>
        {
            entity.ToTable("UserDepartments");
            entity.HasKey(ud => new { ud.UserId, ud.DepartmentId });
            entity
                .HasOne(ud => ud.User)
                .WithMany(u => u.UserDepartments)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(ud => ud.Department)
                .WithMany()
                .HasForeignKey(ud => ud.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.Name).IsUnique();
            entity.Property(d => d.Name).HasMaxLength(256);
            entity.Property(d => d.Description).HasMaxLength(1024);
            entity.Property(d => d.CalculationType).HasConversion<string>().HasMaxLength(64);
            entity.Property(d => d.GoalBonusPercentage).HasPrecision(18, 4);
            entity.Property(d => d.LowRevenueThreshold).HasPrecision(18, 2);
            entity.Property(d => d.LowRevenueBonusPct).HasPrecision(18, 4);
        });

        ConfigureCareerLevel(builder.Entity<CareerLevel>());

        builder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(256);
            entity.Property(p => p.Client).HasMaxLength(256);
            entity.Property(p => p.Platform).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<ProjectRevenue>(entity =>
        {
            entity.ToTable("ProjectRevenues");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.ProjectId, r.Month, r.Year }).IsUnique();
            entity.Property(r => r.ValueIgaming).HasPrecision(18, 2);
            entity.Property(r => r.ValueVendas).HasPrecision(18, 2);
            entity.Property(r => r.Value).HasPrecision(18, 2);
            entity.Property(r => r.GroupPercentage).HasPrecision(18, 4);
            entity.Property(r => r.Notes).HasMaxLength(2048);
            entity
                .HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AnalystMetric>(entity =>
        {
            entity.ToTable("AnalystMetrics");
            entity.HasKey(metric => metric.Id);
            entity.HasIndex(metric => new
            {
                metric.CollaboratorId,
                metric.ProjectId,
                metric.Month,
                metric.Year
            }).IsUnique();
            entity
                .HasOne(metric => metric.Collaborator)
                .WithMany()
                .HasForeignKey(metric => metric.CollaboratorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(metric => metric.Project)
                .WithMany()
                .HasForeignKey(metric => metric.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TrafficInvestment>(entity =>
        {
            entity.ToTable("TrafficInvestments");
            entity.HasKey(investment => investment.Id);
            entity.HasIndex(investment => new { investment.ProjectId, investment.Month, investment.Year }).IsUnique();
            entity.Property(investment => investment.MonthlyTarget).HasPrecision(18, 2);
            entity
                .HasOne(investment => investment.Project)
                .WithMany()
                .HasForeignKey(investment => investment.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TrafficInvestmentWeek>(entity =>
        {
            entity.ToTable("TrafficInvestmentWeeks");
            entity.HasKey(week => week.Id);
            entity.HasIndex(week => new { week.TrafficInvestmentId, week.WeekNumber }).IsUnique();
            entity
                .HasOne(week => week.TrafficInvestment)
                .WithMany(investment => investment.Weeks)
                .HasForeignKey(week => week.TrafficInvestmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TrafficWeekDeposit>(entity =>
        {
            entity.ToTable("TrafficWeekDeposits");
            entity.HasKey(deposit => deposit.Id);
            entity.Property(deposit => deposit.RequestedAmount).HasPrecision(18, 2);
            entity.Property(deposit => deposit.DepositedAmount).HasPrecision(18, 2);
            entity.Property(deposit => deposit.Status).HasConversion<string>().HasMaxLength(32);
            entity
                .HasOne(deposit => deposit.Week)
                .WithMany(week => week.Deposits)
                .HasForeignKey(deposit => deposit.TrafficInvestmentWeekId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TrafficWeekChannelSpend>(entity =>
        {
            entity.ToTable("TrafficWeekChannelSpends");
            entity.HasKey(spend => spend.Id);
            entity.HasIndex(spend => new { spend.TrafficInvestmentWeekId, spend.Channel }).IsUnique();
            entity.Property(spend => spend.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Property(spend => spend.Amount).HasPrecision(18, 2);
            entity
                .HasOne(spend => spend.Week)
                .WithMany(week => week.ChannelSpends)
                .HasForeignKey(spend => spend.TrafficInvestmentWeekId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TrafficProjectDeposit>(entity =>
        {
            entity.ToTable("TrafficProjectDeposits");
            entity.HasKey(deposit => deposit.Id);
            entity.HasIndex(deposit => new { deposit.ProjectId, deposit.DepositDate });
            entity.Property(deposit => deposit.DepositDate).HasColumnType("date");
            entity.Property(deposit => deposit.Amount).HasPrecision(18, 2);
            entity.Property(deposit => deposit.Notes).HasMaxLength(2048);
            entity
                .HasOne(deposit => deposit.Project)
                .WithMany()
                .HasForeignKey(deposit => deposit.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(256);
        });

        builder.Entity<ProjectCost>(entity =>
        {
            entity.ToTable("ProjectCosts");
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.Month, c.Year });
            entity.HasIndex(c => new { c.Month, c.Year, c.Type });
            entity.HasIndex(c => c.CompraId);
            entity.HasIndex(c => c.FacilitiesLancamentoId)
                .IsUnique()
                .HasFilter("[FacilitiesLancamentoId] IS NOT NULL");
            entity.Property(c => c.Type).HasConversion<string>().HasMaxLength(16);
            entity.Property(c => c.Category).HasConversion<string>().HasMaxLength(64);
            entity.Property(c => c.Amount).HasPrecision(18, 2);
            entity.Property(c => c.TransactionDate).HasColumnType("date");
            entity.Property(c => c.Requester).HasMaxLength(256);
            entity.Property(c => c.PurchaseLocation).HasMaxLength(256);
            entity.Property(c => c.AttachmentUrl).HasMaxLength(1024);
            entity.Property(c => c.Notes).HasMaxLength(2048);
            entity
                .HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(c => c.PaymentMethod)
                .WithMany()
                .HasForeignKey(c => c.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Collaborator>(entity =>
        {
            entity.ToTable("Collaborators");
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.DepartmentId, c.Name });
            entity.HasIndex(c => new { c.DepartmentId, c.IsActive });
            entity.Property(c => c.Name).HasMaxLength(256);
            entity.Property(c => c.JobTitle).HasMaxLength(256);
            entity.Property(c => c.PixKey).HasMaxLength(256);
            entity.Property(c => c.Email).HasMaxLength(256);
            entity.Property(c => c.PhotoUrl).HasMaxLength(1024);
            entity.Property(c => c.BaseSalary).HasPrecision(18, 2);
            entity.Property(c => c.AdmissionDate).HasColumnType("date");
            entity.Property(c => c.DismissalDate).HasColumnType("date");
            entity.Property(c => c.CalculationProfileOverride)
                .HasConversion<string>()
                .HasMaxLength(64);
            entity
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(c => c.CareerLevel)
                .WithMany()
                .HasForeignKey(c => c.CareerLevelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigurePayroll(builder);

        builder.Entity<SeedEntity>(entity =>
        {
            entity.ToTable("SeedEntities");
            entity.HasKey(s => s.Key);
            entity.Property(s => s.Key).HasMaxLength(128);
        });
    }

    private static void ConfigurePayroll(ModelBuilder builder)
    {
        builder.Entity<Payroll>(entity =>
        {
            entity.ToTable("Payrolls");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.DepartmentId, p.Month, p.Year }).IsUnique();
            entity.HasIndex(p => new { p.Year, p.Month, p.DepartmentId });
            entity.HasIndex(p => new { p.Year, p.Status });
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(p => p.TotalAmount).HasPrecision(18, 2);
            entity.Property(p => p.RejectionComment).HasMaxLength(2048);
            entity.Property(p => p.SubmittedBy).HasMaxLength(256);
            entity.Property(p => p.SubmittedByUserId).HasMaxLength(450);
            entity.Property(p => p.ApprovedBy).HasMaxLength(256);
            entity
                .HasOne(p => p.Department)
                .WithMany()
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Type).HasMaxLength(64);
            entity.Property(n => n.Title).HasMaxLength(256);
            entity.Property(n => n.Message).HasMaxLength(2048);
            entity.Property(n => n.UserId).HasMaxLength(450);
            entity.Property(n => n.RoleTarget).HasMaxLength(64);
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            entity.HasIndex(n => new { n.RoleTarget, n.IsRead, n.CreatedAt });
            entity.HasIndex(n => n.PayrollId);
            entity
                .HasOne(n => n.Payroll)
                .WithMany()
                .HasForeignKey(n => n.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NotificationReadReceipt>(entity =>
        {
            entity.ToTable("NotificationReadReceipts");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.UserId).HasMaxLength(450);
            entity.HasIndex(r => new { r.NotificationId, r.UserId }).IsUnique();
            entity.HasIndex(r => new { r.UserId, r.ReadAt });
            entity
                .HasOne(r => r.Notification)
                .WithMany()
                .HasForeignKey(r => r.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PayrollCollaboratorEntry>(entity =>
        {
            entity.ToTable("PayrollCollaboratorEntries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PayrollId, e.CollaboratorId }).IsUnique();
            entity.Property(e => e.CollaboratorName).HasMaxLength(256);
            entity.Property(e => e.PixKey).HasMaxLength(256);
            entity.Property(e => e.CareerLevelName).HasMaxLength(256);
            entity.Property(e => e.AdmissionDate).HasColumnType("date");
            entity.Property(e => e.CalculationProfile).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.GoalTier).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.FullBaseSalary).HasPrecision(18, 2);
            entity.Property(e => e.FinalSalary).HasPrecision(18, 2);
            entity.Property(e => e.SupervisorAnalystRevenue).HasPrecision(18, 2);
            entity
                .Property(e => e.Payload)
                .HasConversion(
                    payload => JsonSerializer.Serialize(payload, PayrollJsonOptions.Instance),
                    json => JsonSerializer.Deserialize<PayrollCollaboratorEntryPayload>(
                        json,
                        PayrollJsonOptions.Instance) ?? new PayrollCollaboratorEntryPayload())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(PayrollPayloadValueComparer.Instance);
            entity
                .HasOne(e => e.Payroll)
                .WithMany(p => p.Entries)
                .HasForeignKey(e => e.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCareerLevel(EntityTypeBuilder<CareerLevel> entity)
    {
        entity.ToTable("CareerLevels");
        entity.HasKey(c => c.Id);
        entity.HasIndex(c => new { c.DepartmentId, c.Name }).IsUnique();
        entity.Property(c => c.Name).HasMaxLength(256);
        entity.Property(c => c.Profile).HasConversion<string>().HasMaxLength(64);
        entity
            .HasOne(c => c.Department)
            .WithMany(d => d.CareerLevels)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Property(c => c.BaseSalary).HasPrecision(18, 2);
        entity.Property(c => c.CommissionWithoutGoalPct).HasPrecision(18, 4);
        entity.Property(c => c.CommissionWithGoalPct).HasPrecision(18, 4);
        entity.Property(c => c.CommissionWithSuperGoalPct).HasPrecision(18, 4);
        entity.Property(c => c.GroupCommissionPerPercent).HasPrecision(18, 2);
        entity.Property(c => c.GroupCommissionPer20Percent).HasPrecision(18, 2);
        entity.Property(c => c.DefaultCpaValue).HasPrecision(18, 2);
        entity.Property(c => c.GoalBonusValue).HasPrecision(18, 2);
        entity.Property(c => c.FtdRateBase).HasPrecision(18, 2);
        entity.Property(c => c.FtdRateWithGoal).HasPrecision(18, 2);
        entity.Property(c => c.FtdRateWithSuperGoal).HasPrecision(18, 2);
        entity.Property(c => c.FtdSuperbetRate).HasPrecision(18, 2);
        entity.Property(c => c.FtdBonusValue).HasPrecision(18, 2);
        entity.Property(c => c.SalesPctBase).HasPrecision(18, 4);
        entity.Property(c => c.SalesPctWithGoal).HasPrecision(18, 4);
        entity.Property(c => c.SalesPctWithSuperGoal).HasPrecision(18, 4);
        entity.Property(c => c.SalesBonusEvery).HasPrecision(18, 2);
        entity.Property(c => c.SalesBonusValue).HasPrecision(18, 2);
        entity.Property(c => c.RevPct).HasPrecision(18, 4);
        entity.Property(c => c.BetanoInternaValue).HasPrecision(18, 2);
        entity.Property(c => c.BetanoMundoBetValue).HasPrecision(18, 2);
        entity.Property(c => c.SupFtdSuperbetNoGoal).HasPrecision(18, 2);
        entity.Property(c => c.SupFtdSuperbetWithGoal).HasPrecision(18, 2);
        entity.Property(c => c.SupFtdOtherNoGoal).HasPrecision(18, 2);
        entity.Property(c => c.SupFtdOtherWithGoal).HasPrecision(18, 2);
        entity.Property(c => c.SupSalesPctNoGoal).HasPrecision(18, 4);
        entity.Property(c => c.SupSalesPctWithGoal).HasPrecision(18, 4);
        entity.Property(c => c.SupRevPct).HasPrecision(18, 4);
        entity.Property(c => c.NetRevenueFactor).HasPrecision(18, 4);
        entity.Property(c => c.NetRevenuePctNoGoal).HasPrecision(18, 4);
        entity.Property(c => c.NetRevenuePctWithGoal).HasPrecision(18, 4);
        entity.Property(c => c.TrafficInvestmentCommissionPct).HasPrecision(18, 4);
        entity.Property(c => c.TrafficCpaEsportiva).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaStake).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaBetano).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaBetMgm).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaNovibet).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaBetFair).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaBlaze).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaSuperbet).HasPrecision(18, 2);
        entity.Property(c => c.TrafficCpaHiperbet).HasPrecision(18, 2);
        entity.Property(c => c.TrafficSupBonus).HasPrecision(18, 2);
        entity.Property(c => c.TrafficSupCommissionPct).HasPrecision(18, 4);
    }
}
