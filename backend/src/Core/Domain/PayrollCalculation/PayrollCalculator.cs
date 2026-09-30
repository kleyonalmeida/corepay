using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Orquestrador do motor de cálculo da folha (REGRAS §5–6).
/// </summary>
public sealed class PayrollCalculator
{
    public Result<PayrollBatchResult> RecalcAllEntries(IReadOnlyList<PayrollEntryInput> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var results = new List<PayrollEntryResult>(entries.Count);
        foreach (var entry in entries)
        {
            var result = CalcEntry(entry);
            if (result.IsFailure)
            {
                return Result<PayrollBatchResult>.Failure(result.Error!);
            }

            results.Add(result.Value);
        }

        var total = results.Sum(r => r.TotalAmount);
        return Result<PayrollBatchResult>.Success(new PayrollBatchResult
        {
            EntryResults = results,
            TotalAmount = total
        });
    }

    public Result<IReadOnlyList<ProjectTotalAllocation>> CalcProjectTotalsForEntry(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        if (input.Department is null && !RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success([]);
        }

        if (RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return RoleChangeProjectTotalsCalculator.Calculate(input, CalcProjectTotalsCore);
        }

        var coreResult = CalcProjectTotalsCore(input);
        if (coreResult.IsFailure)
        {
            return coreResult;
        }

        var accumulator = new ProjectTotalsAccumulator();
        accumulator.AddRange(coreResult.Value);
        ProjectTotalsMerger.ApplyManualBonuses(accumulator, input.BonusEntries);

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }

    public Result<IReadOnlyList<ProjectTotalAllocation>> GetDisplayProjectEntries(
        PayrollEntryInput input,
        Guid affiliatesProjectId)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        if (input.Department is null && !RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success([]);
        }

        if (RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return GetDisplayProjectEntriesForRoleChange(input, affiliatesProjectId);
        }

        var totalsResult = CalcProjectTotalsForEntry(input);
        if (totalsResult.IsFailure)
        {
            return totalsResult;
        }

        var entryResult = CalcEntry(input);
        if (entryResult.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(entryResult.Error!);
        }

        return ProjectDisplayHelper.Transform(
            input,
            totalsResult.Value,
            entryResult.Value,
            affiliatesProjectId);
    }

    private Result<IReadOnlyList<ProjectTotalAllocation>> GetDisplayProjectEntriesForRoleChange(
        PayrollEntryInput input,
        Guid affiliatesProjectId)
    {
        var periodsResult = RoleChangePeriodSplitter.Build(input);
        if (periodsResult.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(periodsResult.Error!);
        }

        var periodDisplays = new List<IReadOnlyList<ProjectTotalAllocation>>();
        foreach (var period in periodsResult.Value)
        {
            var periodInput = RoleChangePeriodSplitter.ToPeriodInput(input, period);
            var coreResult = CalcProjectTotalsCore(periodInput);
            if (coreResult.IsFailure)
            {
                return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(coreResult.Error!);
            }

            var entryResult = CalcEntryCore(periodInput);
            if (entryResult.IsFailure)
            {
                return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(entryResult.Error!);
            }

            var transformResult = ProjectDisplayHelper.Transform(
                periodInput,
                coreResult.Value,
                entryResult.Value,
                affiliatesProjectId);
            if (transformResult.IsFailure)
            {
                return transformResult;
            }

            periodDisplays.Add(transformResult.Value);
        }

        var merged = ProjectTotalsMerger.Merge(periodDisplays);
        var accumulator = new ProjectTotalsAccumulator();
        accumulator.AddRange(merged);
        ProjectTotalsMerger.ApplyManualBonuses(accumulator, input.BonusEntries);

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }

    public Result<PayrollEntryResult> CalcEntry(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        if (input.Department is null && !RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return Result<PayrollEntryResult>.Success(PayrollEntryResult.Zero);
        }

        if (RoleChangePayrollCalculator.HasRoleChangesInMonth(input))
        {
            return RoleChangePayrollCalculator.Calculate(input, CalcEntryCore);
        }

        return CalcEntryCore(input);
    }

    internal Result<PayrollEntryResult> CalcEntryCore(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        if (input.Department is null)
        {
            return Result<PayrollEntryResult>.Success(PayrollEntryResult.Zero);
        }

        var profile = CalculationProfileResolver.Resolve(
            input.Collaborator,
            input.Department,
            input.CareerLevel);

        return profile switch
        {
            CalculationProfile.PaidTraffic =>
                PaidTrafficCalculator.Calculate(input),
            CalculationProfile.CommercialSupervisor =>
                Result<PayrollEntryResult>.Success(CommercialSupervisorCalculator.Calculate(input)),
            CalculationProfile.CommercialAnalyst =>
                Result<PayrollEntryResult>.Success(CommercialAnalystCalculator.Calculate(input)),
            CalculationProfile.Management =>
                Result<PayrollEntryResult>.Success(ManagementCalculator.Calculate(input)),
            CalculationProfile.ProjectLeader =>
                Result<PayrollEntryResult>.Success(ProjectLeaderCalculator.Calculate(input)),
            CalculationProfile.CommissionOnly =>
                Result<PayrollEntryResult>.Success(CommissionOnlyCalculator.Calculate(input)),
            CalculationProfile.FixedCommission =>
                Result<PayrollEntryResult>.Success(FixedCommissionCalculator.Calculate(input)),
            CalculationProfile.FixedCommissionBonus =>
                Result<PayrollEntryResult>.Success(FixedCommissionBonusCalculator.Calculate(input)),
            CalculationProfile.FixedBonus or
            CalculationProfile.Tipster or
            CalculationProfile.AllocatedFixed =>
                Result<PayrollEntryResult>.Success(
                    FixedBonusSectionCalculator.Calculate(input, profile)),
            _ => Result<PayrollEntryResult>.Failure(
                Error.Validation(
                    "payroll.profile_not_implemented",
                    $"Perfil de cálculo '{profile}' ainda não implementado."))
        };
    }

    internal Result<IReadOnlyList<ProjectTotalAllocation>> CalcProjectTotalsCore(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        if (input.Department is null)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success([]);
        }

        var profile = CalculationProfileResolver.Resolve(
            input.Collaborator,
            input.Department,
            input.CareerLevel);

        return profile switch
        {
            CalculationProfile.PaidTraffic =>
                PaidTrafficProjectTotalsCalculator.Calculate(input),
            CalculationProfile.CommercialSupervisor =>
                Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                    CommercialSupervisorProjectTotalsCalculator.Calculate(input)),
            CalculationProfile.CommercialAnalyst =>
                CommercialAnalystProjectTotalsCalculator.Calculate(input),
            CalculationProfile.Management =>
                Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                    ManagementProjectTotalsCalculator.Calculate(input)),
            CalculationProfile.ProjectLeader =>
                Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                    ProjectLeaderProjectTotalsCalculator.Calculate(input)),
            CalculationProfile.FixedBonus or
            CalculationProfile.Tipster or
            CalculationProfile.AllocatedFixed =>
                Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                    FixedBonusSectionProjectTotalsCalculator.Calculate(input, profile)),
            CalculationProfile.CommissionOnly or
            CalculationProfile.FixedCommission or
            CalculationProfile.FixedCommissionBonus =>
                Result<IReadOnlyList<ProjectTotalAllocation>>.Success([]),
            _ => Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(
                Error.Validation(
                    "payroll.profile_not_implemented",
                    $"Perfil de cálculo '{profile}' ainda não implementado."))
        };
    }
}
