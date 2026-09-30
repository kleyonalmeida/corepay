using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Divide a competência em períodos inclusivos por mudança de cargo (REGRAS §5.2).
/// </summary>
public static class RoleChangePeriodSplitter
{
    public static Result<IReadOnlyList<PayrollPeriodDefinition>> Build(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        var monthStart = new DateOnly(input.Year, input.Month, 1);
        var monthEnd = new DateOnly(
            input.Year,
            input.Month,
            DateTime.DaysInMonth(input.Year, input.Month));

        var changesInMonth = input.RoleChanges
            .Where(change => change.ChangeDate >= monthStart && change.ChangeDate <= monthEnd)
            .OrderBy(change => change.ChangeDate)
            .ToList();

        if (changesInMonth.Count == 0)
        {
            return Result<IReadOnlyList<PayrollPeriodDefinition>>.Failure(
                Error.Validation(
                    "payroll.role_change_required",
                    "Nenhuma mudança de cargo válida encontrada na competência."));
        }

        if (changesInMonth
            .GroupBy(change => change.ChangeDate)
            .Any(group => group.Count() > 1))
        {
            return Result<IReadOnlyList<PayrollPeriodDefinition>>.Failure(
                Error.Validation(
                    "payroll.role_change_duplicate_date",
                    "Não é permitido registrar mais de uma mudança de cargo na mesma data."));
        }

        var periods = new List<PayrollPeriodDefinition>();
        var mainRole = PayrollRoleSnapshot.FromMainInput(input);
        var lastPeriodEnd = ResolveLastPeriodEnd(input, monthStart, monthEnd);

        if (changesInMonth[0].ChangeDate > monthStart)
        {
            periods.Add(new PayrollPeriodDefinition(
                monthStart,
                changesInMonth[0].ChangeDate.AddDays(-1),
                mainRole));
        }

        for (var index = 0; index < changesInMonth.Count; index++)
        {
            var periodStart = changesInMonth[index].ChangeDate;
            var periodEnd = index < changesInMonth.Count - 1
                ? changesInMonth[index + 1].ChangeDate.AddDays(-1)
                : lastPeriodEnd;

            periods.Add(new PayrollPeriodDefinition(
                periodStart,
                periodEnd,
                changesInMonth[index].Role));
        }

        var validPeriods = periods
            .Where(period => period.StartInclusive <= period.EndInclusive)
            .ToList();

        return Result<IReadOnlyList<PayrollPeriodDefinition>>.Success(validPeriods);
    }

    public static PayrollEntryInput ToPeriodInput(
        PayrollEntryInput source,
        PayrollPeriodDefinition period) =>
        source with
        {
            Department = period.Role.Department,
            CareerLevel = period.Role.CareerLevel,
            FullBaseSalary = period.Role.FullBaseSalary,
            GoalTier = period.Role.GoalTier,
            FinalSalary = period.Role.FinalSalary,
            TrafficSeniorLevel = period.Role.TrafficSeniorLevel,
            ManagementRevenueEntries = period.Role.ManagementRevenueEntries,
            TrafficProjectEntries = period.Role.TrafficProjectEntries,
            SupervisorAnalystRevenue = period.Role.SupervisorAnalystRevenue,
            SupervisorProjectEntries = period.Role.SupervisorProjectEntries,
            CommercialProjectEntries = period.Role.CommercialProjectEntries,
            BetanoInternaCount = period.Role.BetanoInternaCount,
            BetanoMundoBetCount = period.Role.BetanoMundoBetCount,
            ProjectEntries = period.Role.ProjectEntries,
            RateioProjectEntries = period.Role.RateioProjectEntries,
            ProjectSnapshots = period.Role.ProjectSnapshots,
            CommissionPayingProjectId = period.Role.CommissionPayingProjectId,
            ComplementPayingProjects = period.Role.ComplementPayingProjects,
            PeriodClipStart = period.StartInclusive,
            PeriodClipEnd = period.EndInclusive,
            RoleChanges = [],
            BonusEntries = [],
            DeductionEntries = []
        };

    private static DateOnly ResolveLastPeriodEnd(
        PayrollEntryInput input,
        DateOnly monthStart,
        DateOnly monthEnd)
    {
        var dismissal = input.Collaborator.DismissalDate;
        if (dismissal.HasValue
            && dismissal.Value >= monthStart
            && dismissal.Value <= monthEnd)
        {
            return dismissal.Value;
        }

        return monthEnd;
    }
}
