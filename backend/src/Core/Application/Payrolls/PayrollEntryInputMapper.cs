using BuildingBlocks.Results;
using Core.Domain;
using Core.Domain.PayrollCalculation;

namespace Core.Application.Payrolls;

public static class PayrollEntryInputMapper
{
    public static Result<PayrollEntryInput> Map(
        Payroll payroll,
        PayrollCollaboratorEntry entry,
        Collaborator collaborator,
        Department department,
        CareerLevel? careerLevel,
        CareerLevel? trafficSeniorLevel,
        IReadOnlyDictionary<Guid, Project> activeProjectsById,
        IReadOnlyDictionary<Guid, Department> departmentsById,
        IReadOnlyDictionary<Guid, CareerLevel> careerLevelsById,
        PreviewPayrollEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(payroll);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(collaborator);
        ArgumentNullException.ThrowIfNull(department);
        ArgumentNullException.ThrowIfNull(request);

        var projectValidation = ValidateProjectReferences(request, activeProjectsById);
        if (projectValidation.IsFailure)
        {
            return Result<PayrollEntryInput>.Failure(projectValidation.Error!);
        }

        var projectSnapshots = activeProjectsById.Values
            .Select(p => new ProjectCalculationSnapshot(
                p.Id,
                p.ExcludesGoalBonus,
                p.IsDefaultAllocationTarget,
                p.ExcludesSupervisorFixedAllocation))
            .ToList();

        var commercialEntriesResult = EnrichCommercialEntries(
            request.CommercialProjectEntries ?? [],
            activeProjectsById);
        if (commercialEntriesResult.IsFailure)
        {
            return Result<PayrollEntryInput>.Failure(commercialEntriesResult.Error!);
        }

        var roleChangesResult = MapRoleChanges(
            request.RoleChanges ?? [],
            activeProjectsById,
            departmentsById,
            careerLevelsById,
            projectSnapshots);
        if (roleChangesResult.IsFailure)
        {
            return Result<PayrollEntryInput>.Failure(roleChangesResult.Error!);
        }

        var input = new PayrollEntryInput
        {
            Month = payroll.Month,
            Year = payroll.Year,
            Department = department,
            CareerLevel = careerLevel,
            Collaborator = collaborator,
            FullBaseSalary = entry.FullBaseSalary,
            GoalTier = request.GoalTier,
            FinalSalary = request.FinalSalary,
            ManagementRevenueEntries = request.ManagementRevenueEntries ?? [],
            TrafficSeniorLevel = trafficSeniorLevel,
            TrafficProjectEntries = request.TrafficProjectEntries ?? [],
            SupervisorAnalystRevenue = request.SupervisorAnalystRevenue,
            SupervisorProjectEntries = request.SupervisorProjectEntries ?? [],
            CommercialProjectEntries = commercialEntriesResult.Value!,
            BetanoInternaCount = request.BetanoInternaCount,
            BetanoMundoBetCount = request.BetanoMundoBetCount,
            ProjectEntries = request.ProjectEntries ?? [],
            RateioProjectEntries = request.RateioProjectEntries ?? [],
            ProjectSnapshots = projectSnapshots,
            BonusEntries = request.BonusEntries ?? [],
            DeductionEntries = request.DeductionEntries ?? [],
            CommissionPayingProjectId = request.CommissionPayingProjectId,
            ComplementPayingProjects = request.ComplementPayingProjects ?? [],
            RoleChanges = roleChangesResult.Value!
        };

        return Result<PayrollEntryInput>.Success(input);
    }

    public static PayrollEntryPayloadResponse MapPayload(PayrollCollaboratorEntry entry) =>
        new(
            entry.Payload.ProjectEntries,
            entry.Payload.RateioProjectEntries,
            entry.Payload.CommercialProjectEntries,
            entry.Payload.SupervisorProjectEntries,
            entry.Payload.TrafficProjectEntries,
            entry.Payload.ManagementRevenueEntries,
            entry.Payload.BonusEntries,
            entry.Payload.DeductionEntries,
            entry.Payload.ComplementPayingProjects,
            entry.Payload.RoleChanges);

    public static void ApplyRequest(PayrollCollaboratorEntry entry, PreviewPayrollEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(request);

        entry.GoalTier = request.GoalTier;
        entry.FinalSalary = request.FinalSalary;
        entry.BetanoInternaCount = request.BetanoInternaCount;
        entry.BetanoMundoBetCount = request.BetanoMundoBetCount;
        entry.SupervisorAnalystRevenue = request.SupervisorAnalystRevenue;
        entry.CommissionPayingProjectId = request.CommissionPayingProjectId;
        entry.Payload.ProjectEntries = request.ProjectEntries ?? [];
        entry.Payload.RateioProjectEntries = request.RateioProjectEntries ?? [];
        entry.Payload.CommercialProjectEntries = request.CommercialProjectEntries ?? [];
        entry.Payload.SupervisorProjectEntries = request.SupervisorProjectEntries ?? [];
        entry.Payload.TrafficProjectEntries = request.TrafficProjectEntries ?? [];
        entry.Payload.ManagementRevenueEntries = request.ManagementRevenueEntries ?? [];
        entry.Payload.BonusEntries = request.BonusEntries ?? [];
        entry.Payload.DeductionEntries = request.DeductionEntries ?? [];
        entry.Payload.ComplementPayingProjects = request.ComplementPayingProjects ?? [];
        entry.Payload.RoleChanges = request.RoleChanges ?? [];
    }

    public static PreviewPayrollEntryRequest ToPreviewRequest(PayrollCollaboratorEntry entry) =>
        new(
            entry.GoalTier,
            entry.FinalSalary,
            entry.BetanoInternaCount,
            entry.BetanoMundoBetCount,
            entry.SupervisorAnalystRevenue,
            entry.CommissionPayingProjectId,
            entry.Payload.ManagementRevenueEntries,
            entry.Payload.TrafficProjectEntries,
            entry.Payload.SupervisorProjectEntries,
            entry.Payload.CommercialProjectEntries,
            entry.Payload.ProjectEntries,
            entry.Payload.RateioProjectEntries,
            entry.Payload.BonusEntries,
            entry.Payload.DeductionEntries,
            entry.Payload.ComplementPayingProjects,
            entry.Payload.RoleChanges);

    public static PreviewPayrollEntryRequest ToPreviewRequest(UpdatePayrollEntryRequest request) =>
        new(
            request.GoalTier,
            request.FinalSalary,
            request.BetanoInternaCount,
            request.BetanoMundoBetCount,
            request.SupervisorAnalystRevenue,
            request.CommissionPayingProjectId,
            request.ManagementRevenueEntries,
            request.TrafficProjectEntries,
            request.SupervisorProjectEntries,
            request.CommercialProjectEntries,
            request.ProjectEntries,
            request.RateioProjectEntries,
            request.BonusEntries,
            request.DeductionEntries,
            request.ComplementPayingProjects,
            request.RoleChanges);

    public static PayrollEntryEditorResponse MapEditorEntry(
        PayrollCollaboratorEntry entry,
        PayrollEntryResultResponse? result = null,
        IReadOnlyList<PayrollProjectTotalResponse>? projectTotals = null) =>
        new(
            entry.Id,
            entry.CollaboratorId,
            entry.CollaboratorName,
            entry.CareerLevelName,
            entry.PixKey,
            entry.AdmissionDate,
            entry.FullBaseSalary,
            entry.CalculationProfile,
            entry.IsApproved,
            entry.IsPaid,
            entry.NfSent,
            entry.GoalTier,
            entry.FinalSalary,
            entry.BetanoInternaCount,
            entry.BetanoMundoBetCount,
            entry.SupervisorAnalystRevenue,
            entry.CommissionPayingProjectId,
            MapPayload(entry),
            result,
            projectTotals);

    private static Result<IReadOnlyList<CommercialAnalystProjectEntryInput>> EnrichCommercialEntries(
        IReadOnlyList<CommercialAnalystProjectEntryInput> entries,
        IReadOnlyDictionary<Guid, Project> projectsById)
    {
        var enriched = new List<CommercialAnalystProjectEntryInput>(entries.Count);
        foreach (var entry in entries)
        {
            if (!projectsById.TryGetValue(entry.ProjectId, out var project))
            {
                return Result<IReadOnlyList<CommercialAnalystProjectEntryInput>>.Failure(
                    ProjectNotFoundError());
            }

            enriched.Add(entry with { Platform = project.Platform });
        }

        return Result<IReadOnlyList<CommercialAnalystProjectEntryInput>>.Success(enriched);
    }

    private static Result ValidateProjectReferences(
        PreviewPayrollEntryRequest request,
        IReadOnlyDictionary<Guid, Project> activeProjectsById)
    {
        foreach (var projectId in EnumerateProjectIds(request))
        {
            if (projectId == Guid.Empty || !activeProjectsById.ContainsKey(projectId))
            {
                return Result.Failure(ProjectNotFoundError());
            }
        }

        return Result.Success();
    }

    private static IEnumerable<Guid> EnumerateProjectIds(PreviewPayrollEntryRequest request)
    {
        if (request.CommissionPayingProjectId is Guid commissionProjectId)
        {
            yield return commissionProjectId;
        }

        foreach (var projectId in EnumerateProjectIds(
                     request.ProjectEntries ?? [],
                     request.RateioProjectEntries ?? [],
                     request.TrafficProjectEntries ?? [],
                     request.SupervisorProjectEntries ?? [],
                     request.CommercialProjectEntries ?? [],
                     request.ManagementRevenueEntries ?? [],
                     request.BonusEntries ?? [],
                     request.ComplementPayingProjects ?? []))
        {
            yield return projectId;
        }

        foreach (var change in request.RoleChanges ?? [])
        {
            var role = change.Role;
            if (role.CommissionPayingProjectId is Guid roleCommissionProjectId)
            {
                yield return roleCommissionProjectId;
            }

            foreach (var projectId in EnumerateProjectIds(
                         role.ProjectEntries,
                         role.RateioProjectEntries,
                         role.TrafficProjectEntries,
                         role.SupervisorProjectEntries,
                         role.CommercialProjectEntries,
                         role.ManagementRevenueEntries,
                         [],
                         role.ComplementPayingProjects))
            {
                yield return projectId;
            }
        }
    }

    private static IEnumerable<Guid> EnumerateProjectIds(
        IReadOnlyList<ProjectEntryInput> projectEntries,
        IReadOnlyList<RateioProjectEntryInput> rateioEntries,
        IReadOnlyList<TrafficProjectEntryInput> trafficEntries,
        IReadOnlyList<SupervisorProjectEntryInput> supervisorEntries,
        IReadOnlyList<CommercialAnalystProjectEntryInput> commercialEntries,
        IReadOnlyList<ManagementRevenueEntryInput> managementEntries,
        IReadOnlyList<BonusEntryInput> bonusEntries,
        IReadOnlyList<ComplementPayingProjectInput> complementEntries)
    {
        foreach (var projectId in projectEntries.Select(entry => entry.ProjectId)
                     .Concat(rateioEntries.Select(entry => entry.ProjectId))
                     .Concat(trafficEntries.Select(entry => entry.ProjectId))
                     .Concat(supervisorEntries.Select(entry => entry.ProjectId))
                     .Concat(commercialEntries.Select(entry => entry.ProjectId))
                     .Concat(managementEntries.SelectMany(entry =>
                         entry.ProjectBreakdown.Select(breakdown => breakdown.ProjectId)))
                     .Concat(bonusEntries.Where(entry => entry.ProjectId.HasValue)
                         .Select(entry => entry.ProjectId!.Value))
                     .Concat(complementEntries.Select(entry => entry.ProjectId)))
        {
            yield return projectId;
        }
    }

    private static Error ProjectNotFoundError() =>
        Error.NotFound("payrolls.project_not_found", "Project not found or inactive.");

    private static Result<IReadOnlyList<RoleChangeEntryInput>> MapRoleChanges(
        IReadOnlyList<PayrollRoleChangeEntry> roleChanges,
        IReadOnlyDictionary<Guid, Project> projectsById,
        IReadOnlyDictionary<Guid, Department> departmentsById,
        IReadOnlyDictionary<Guid, CareerLevel> careerLevelsById,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots)
    {
        var mapped = new List<RoleChangeEntryInput>(roleChanges.Count);
        foreach (var change in roleChanges)
        {
            Department? department = null;
            if (change.Role.DepartmentId is not null
                && !departmentsById.TryGetValue(change.Role.DepartmentId.Value, out department))
            {
                return Result<IReadOnlyList<RoleChangeEntryInput>>.Failure(
                    Error.NotFound("payrolls.department_not_found", "Department not found."));
            }

            CareerLevel? careerLevel = null;
            if (change.Role.CareerLevelId is not null
                && !careerLevelsById.TryGetValue(change.Role.CareerLevelId.Value, out careerLevel))
            {
                return Result<IReadOnlyList<RoleChangeEntryInput>>.Failure(
                    Error.NotFound("payrolls.career_level_not_found", "Career level not found."));
            }

            CareerLevel? trafficSeniorLevel = null;
            if (change.Role.TrafficSeniorLevelId is not null
                && !careerLevelsById.TryGetValue(change.Role.TrafficSeniorLevelId.Value, out trafficSeniorLevel))
            {
                return Result<IReadOnlyList<RoleChangeEntryInput>>.Failure(
                    Error.NotFound("payrolls.career_level_not_found", "Career level not found."));
            }

            var commercialResult = EnrichCommercialEntries(
                change.Role.CommercialProjectEntries,
                projectsById);
            if (commercialResult.IsFailure)
            {
                return Result<IReadOnlyList<RoleChangeEntryInput>>.Failure(commercialResult.Error!);
            }

            mapped.Add(new RoleChangeEntryInput(
                change.ChangeDate,
                new PayrollRoleSnapshot
                {
                    Department = department,
                    CareerLevel = careerLevel,
                    FullBaseSalary = change.Role.FullBaseSalary,
                    GoalTier = change.Role.GoalTier,
                    FinalSalary = change.Role.FinalSalary,
                    TrafficSeniorLevel = trafficSeniorLevel,
                    ManagementRevenueEntries = change.Role.ManagementRevenueEntries,
                    TrafficProjectEntries = change.Role.TrafficProjectEntries,
                    SupervisorAnalystRevenue = change.Role.SupervisorAnalystRevenue,
                    SupervisorProjectEntries = change.Role.SupervisorProjectEntries,
                    CommercialProjectEntries = commercialResult.Value!,
                    BetanoInternaCount = change.Role.BetanoInternaCount,
                    BetanoMundoBetCount = change.Role.BetanoMundoBetCount,
                    ProjectEntries = change.Role.ProjectEntries,
                    RateioProjectEntries = change.Role.RateioProjectEntries,
                    ProjectSnapshots = projectSnapshots,
                    CommissionPayingProjectId = change.Role.CommissionPayingProjectId,
                    ComplementPayingProjects = change.Role.ComplementPayingProjects
                }));
        }

        return Result<IReadOnlyList<RoleChangeEntryInput>>.Success(mapped);
    }
}
