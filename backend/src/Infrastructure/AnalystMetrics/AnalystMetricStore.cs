using BuildingBlocks.Results;
using Core.Application.AnalystMetrics;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AnalystMetrics;

public sealed class AnalystMetricStore(AppDbContext dbContext) : IAnalystMetricStore
{
    public async Task<Result<IReadOnlyList<AnalystMetricResponse>>> GetListAsync(
        AnalystMetricListFilters filters,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateListFilters(filters, access);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<AnalystMetricResponse>>.Failure(validation.Error!);
        }

        var query = dbContext.AnalystMetrics
            .AsNoTracking()
            .Include(metric => metric.Collaborator)
                .ThenInclude(collaborator => collaborator.Department)
            .Include(metric => metric.Project)
            .AsQueryable();

        if (access.AllowedDepartmentIds is not null)
        {
            query = query.Where(metric =>
                access.AllowedDepartmentIds.Contains(metric.Collaborator.DepartmentId));
        }

        if (filters.Month is not null)
        {
            query = query.Where(metric => metric.Month == filters.Month);
        }

        if (filters.Year is not null)
        {
            query = query.Where(metric => metric.Year == filters.Year);
        }

        if (filters.DepartmentId is not null)
        {
            query = query.Where(metric => metric.Collaborator.DepartmentId == filters.DepartmentId);
        }

        if (filters.CollaboratorId is not null)
        {
            query = query.Where(metric => metric.CollaboratorId == filters.CollaboratorId);
        }

        if (filters.ProjectId is not null)
        {
            query = query.Where(metric => metric.ProjectId == filters.ProjectId);
        }

        var metrics = await query
            .OrderByDescending(metric => metric.Year)
            .ThenByDescending(metric => metric.Month)
            .ThenBy(metric => metric.Collaborator.Name)
            .ThenBy(metric => metric.Project.Name)
            .Select(metric => MapResponse(metric))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AnalystMetricResponse>>.Success(metrics);
    }

    public async Task<Result<AnalystMetricResponse>> GetByIdAsync(
        Guid analystMetricId,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var metric = await LoadAsync(analystMetricId, tracking: false, cancellationToken);
        if (metric is null)
        {
            return NotFound();
        }

        if (!CanAccess(metric.Collaborator.DepartmentId, access))
        {
            return Forbidden();
        }

        return Result<AnalystMetricResponse>.Success(MapResponse(metric));
    }

    public async Task<Result<AnalystMetricResponse>> CreateAsync(
        CreateAnalystMetricRequest request,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateMutationAsync(
            request.CollaboratorId,
            request.ProjectId,
            request.Month,
            request.Year,
            request.FtdTotal,
            request.CpaCount,
            excludeId: null,
            access,
            cancellationToken);
        if (validation.IsFailure)
        {
            return Result<AnalystMetricResponse>.Failure(validation.Error!);
        }

        var metric = new AnalystMetric
        {
            Id = Guid.NewGuid(),
            CollaboratorId = request.CollaboratorId,
            ProjectId = request.ProjectId,
            Month = request.Month,
            Year = request.Year,
            FtdTotal = request.FtdTotal,
            CpaCount = request.CpaCount
        };

        dbContext.AnalystMetrics.Add(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await LoadAsync(metric.Id, tracking: false, cancellationToken);
        return Result<AnalystMetricResponse>.Success(MapResponse(created!));
    }

    public async Task<Result<AnalystMetricResponse>> UpdateAsync(
        Guid analystMetricId,
        UpdateAnalystMetricRequest request,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var metric = await LoadAsync(analystMetricId, tracking: true, cancellationToken);
        if (metric is null)
        {
            return NotFound();
        }

        if (!CanAccess(metric.Collaborator.DepartmentId, access))
        {
            return Forbidden();
        }

        var validation = await ValidateMutationAsync(
            request.CollaboratorId,
            request.ProjectId,
            request.Month,
            request.Year,
            request.FtdTotal,
            request.CpaCount,
            analystMetricId,
            access,
            cancellationToken);
        if (validation.IsFailure)
        {
            return Result<AnalystMetricResponse>.Failure(validation.Error!);
        }

        metric.CollaboratorId = request.CollaboratorId;
        metric.ProjectId = request.ProjectId;
        metric.Month = request.Month;
        metric.Year = request.Year;
        metric.FtdTotal = request.FtdTotal;
        metric.CpaCount = request.CpaCount;

        await dbContext.SaveChangesAsync(cancellationToken);
        var updated = await LoadAsync(metric.Id, tracking: false, cancellationToken);
        return Result<AnalystMetricResponse>.Success(MapResponse(updated!));
    }

    private static Result ValidateListFilters(
        AnalystMetricListFilters filters,
        AnalystMetricAccessContext access)
    {
        var competence = ValidateCompetence(filters.Month, filters.Year);
        if (competence.IsFailure)
        {
            return competence;
        }

        if (filters.DepartmentId is not null
            && access.AllowedDepartmentIds is not null
            && !access.AllowedDepartmentIds.Contains(filters.DepartmentId.Value))
        {
            return Result.Failure(
                Error.Forbidden(
                    "analystmetrics.department_forbidden",
                    "Department is outside your scope."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateMutationAsync(
        Guid collaboratorId,
        Guid projectId,
        int month,
        int year,
        int ftdTotal,
        int cpaCount,
        Guid? excludeId,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken)
    {
        var competence = ValidateCompetence(month, year);
        if (competence.IsFailure)
        {
            return competence;
        }

        if (ftdTotal < 0 || cpaCount < 0)
        {
            return Result.Failure(
                Error.Validation(
                    "analystmetrics.invalid_counts",
                    "FTD and CPA counts cannot be negative."));
        }

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == collaboratorId, cancellationToken);
        if (collaborator is null)
        {
            return Result.Failure(
                Error.Validation(
                    "analystmetrics.collaborator_not_found",
                    "Collaborator not found."));
        }

        if (!CanAccess(collaborator.DepartmentId, access))
        {
            return Result.Failure(
                Error.Forbidden(
                    "analystmetrics.department_forbidden",
                    "Collaborator is outside your department scope."));
        }

        if (!await dbContext.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return Result.Failure(
                Error.Validation("analystmetrics.project_not_found", "Project not found."));
        }

        var duplicateQuery = dbContext.AnalystMetrics.Where(metric =>
            metric.CollaboratorId == collaboratorId
            && metric.ProjectId == projectId
            && metric.Month == month
            && metric.Year == year);
        if (excludeId is not null)
        {
            duplicateQuery = duplicateQuery.Where(metric => metric.Id != excludeId);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            return Result.Failure(
                Error.Conflict(
                    "analystmetrics.duplicate",
                    "A metric already exists for this collaborator, project and competence."));
        }

        return Result.Success();
    }

    private static Result ValidateCompetence(int? month, int? year)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("analystmetrics.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("analystmetrics.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private async Task<AnalystMetric?> LoadAsync(
        Guid id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AnalystMetrics
            .Include(metric => metric.Collaborator)
                .ThenInclude(collaborator => collaborator.Department)
            .Include(metric => metric.Project)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(metric => metric.Id == id, cancellationToken);
    }

    private static bool CanAccess(Guid departmentId, AnalystMetricAccessContext access) =>
        access.AllowedDepartmentIds is null || access.AllowedDepartmentIds.Contains(departmentId);

    private static AnalystMetricResponse MapResponse(AnalystMetric metric) =>
        new(
            metric.Id,
            metric.CollaboratorId,
            metric.Collaborator.Name,
            metric.Collaborator.DepartmentId,
            metric.Collaborator.Department.Name,
            metric.ProjectId,
            metric.Project.Name,
            metric.Month,
            metric.Year,
            metric.FtdTotal,
            metric.CpaCount);

    private static Result<AnalystMetricResponse> NotFound() =>
        Result<AnalystMetricResponse>.Failure(
            Error.NotFound("analystmetrics.not_found", "Analyst metric not found."));

    private static Result<AnalystMetricResponse> Forbidden() =>
        Result<AnalystMetricResponse>.Failure(
            Error.Forbidden(
                "analystmetrics.department_forbidden",
                "Analyst metric is outside your department scope."));
}
