using BuildingBlocks.Results;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payrolls;

public sealed class EntryCalculationContextLoader(AppDbContext dbContext)
{
    private SharedMasterData? _shared;
    private Dictionary<Guid, Collaborator>? _collaboratorsById;
    private readonly Dictionary<(Guid DepartmentId, bool NeedsDefault), CareerLevel?> _trafficSeniorCache = [];

    public Guid AffiliatesProjectId => _shared?.AffiliatesProjectId ?? Guid.Empty;

    public async Task<Result> EnsureSharedLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_shared is not null)
        {
            return Result.Success();
        }

        var activeProjects = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.IsActive)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

        var departments = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .ToListAsync(cancellationToken);

        var careerLevels = await dbContext.CareerLevels
            .AsNoTracking()
            .Where(level => level.IsActive)
            .ToListAsync(cancellationToken);

        var affiliatesProjectId = activeProjects
            .Where(project => project.Name == "Affiliates")
            .Select(project => project.Id)
            .FirstOrDefault();

        _shared = new SharedMasterData(
            activeProjects,
            activeProjects.ToDictionary(project => project.Id),
            departments.ToDictionary(department => department.Id),
            careerLevels.ToDictionary(level => level.Id),
            affiliatesProjectId == Guid.Empty ? Guid.Empty : affiliatesProjectId);

        return Result.Success();
    }

    public async Task PreloadCollaboratorsAsync(
        IEnumerable<Guid> collaboratorIds,
        CancellationToken cancellationToken = default)
    {
        var missingIds = collaboratorIds
            .Distinct()
            .Where(id => _collaboratorsById?.ContainsKey(id) != true)
            .ToList();

        if (missingIds.Count == 0)
        {
            return;
        }

        var loaded = await dbContext.Collaborators
            .AsNoTracking()
            .Include(collaborator => collaborator.Department)
            .Include(collaborator => collaborator.CareerLevel)
            .Where(collaborator => missingIds.Contains(collaborator.Id))
            .ToListAsync(cancellationToken);

        _collaboratorsById ??= [];
        foreach (var collaborator in loaded)
        {
            _collaboratorsById[collaborator.Id] = collaborator;
        }
    }

    public async Task<Result<EntryCalculationContext>> LoadForEntryAsync(
        PayrollCollaboratorEntry entry,
        Payroll payroll,
        CancellationToken cancellationToken = default)
    {
        var sharedResult = await EnsureSharedLoadedAsync(cancellationToken);
        if (sharedResult.IsFailure)
        {
            return Result<EntryCalculationContext>.Failure(sharedResult.Error!);
        }

        await PreloadCollaboratorsAsync([entry.CollaboratorId], cancellationToken);

        if (_collaboratorsById is null || !_collaboratorsById.TryGetValue(entry.CollaboratorId, out var collaborator))
        {
            return Result<EntryCalculationContext>.Failure(
                Error.NotFound("payrolls.collaborator_not_found", "Collaborator not found."));
        }

        if (!_shared!.DepartmentsById.TryGetValue(payroll.DepartmentId, out var department))
        {
            return Result<EntryCalculationContext>.Failure(
                Error.NotFound("payrolls.department_not_found", "Department not found."));
        }

        CareerLevel? careerLevel = null;
        if (entry.CareerLevelId is not null
            && _shared.CareerLevelsById.TryGetValue(entry.CareerLevelId.Value, out var resolvedCareerLevel))
        {
            careerLevel = resolvedCareerLevel;
        }

        var trafficSeniorLevel = await ResolveTrafficSeniorLevelAsync(
            entry,
            payroll,
            cancellationToken);

        return Result<EntryCalculationContext>.Success(new EntryCalculationContext(
            collaborator,
            department,
            careerLevel,
            trafficSeniorLevel,
            _shared.ActiveProjects,
            _shared.ActiveProjectsById,
            _shared.DepartmentsById,
            _shared.CareerLevelsById));
    }

    private async Task<CareerLevel?> ResolveTrafficSeniorLevelAsync(
        PayrollCollaboratorEntry entry,
        Payroll payroll,
        CancellationToken cancellationToken)
    {
        if (entry.TrafficSeniorLevelId is not null
            && _shared!.CareerLevelsById.TryGetValue(entry.TrafficSeniorLevelId.Value, out var explicitLevel))
        {
            return explicitLevel;
        }

        if (entry.CalculationProfile != CalculationProfile.PaidTraffic)
        {
            return null;
        }

        var cacheKey = (payroll.DepartmentId, NeedsDefault: true);
        if (_trafficSeniorCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var trafficSeniorLevel = await dbContext.CareerLevels
            .AsNoTracking()
            .Where(level => level.DepartmentId == payroll.DepartmentId && level.IsActive)
            .OrderByDescending(level => level.TrafficInvestmentCommissionPct)
            .FirstOrDefaultAsync(cancellationToken);

        _trafficSeniorCache[cacheKey] = trafficSeniorLevel;
        return trafficSeniorLevel;
    }

    private sealed record SharedMasterData(
        IReadOnlyList<Project> ActiveProjects,
        IReadOnlyDictionary<Guid, Project> ActiveProjectsById,
        IReadOnlyDictionary<Guid, Department> DepartmentsById,
        IReadOnlyDictionary<Guid, CareerLevel> CareerLevelsById,
        Guid AffiliatesProjectId);
}

public sealed record EntryCalculationContext(
    Collaborator Collaborator,
    Department Department,
    CareerLevel? CareerLevel,
    CareerLevel? TrafficSeniorLevel,
    IReadOnlyList<Project> ActiveProjects,
    IReadOnlyDictionary<Guid, Project> ActiveProjectsById,
    IReadOnlyDictionary<Guid, Department> DepartmentsById,
    IReadOnlyDictionary<Guid, CareerLevel> CareerLevelsById);
