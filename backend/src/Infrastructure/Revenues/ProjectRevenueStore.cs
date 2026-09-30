using BuildingBlocks.Results;
using Core.Application.Revenues;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Revenues;

public sealed class ProjectRevenueStore : IProjectRevenueStore
{
    private const int NotesMaxLength = 2048;

    private readonly AppDbContext _dbContext;

    public ProjectRevenueStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ProjectRevenueResponse>>> GetListAsync(
        ProjectRevenueListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateListFilters(filters);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<ProjectRevenueResponse>>.Failure(validation.Error!);
        }

        var query = _dbContext.ProjectRevenues
            .AsNoTracking()
            .Include(r => r.Project)
            .AsQueryable();

        if (filters.Month is not null)
        {
            query = query.Where(r => r.Month == filters.Month.Value);
        }

        if (filters.Year is not null)
        {
            query = query.Where(r => r.Year == filters.Year.Value);
        }

        if (filters.ProjectId is not null)
        {
            query = query.Where(r => r.ProjectId == filters.ProjectId.Value);
        }

        var items = await query
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ThenBy(r => r.Project.Name)
            .Select(r => MapResponse(r))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProjectRevenueResponse>>.Success(items);
    }

    public async Task<Result<ProjectRevenueResponse>> GetByIdAsync(
        Guid projectRevenueId,
        CancellationToken cancellationToken = default)
    {
        var revenue = await _dbContext.ProjectRevenues
            .AsNoTracking()
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == projectRevenueId, cancellationToken);

        if (revenue is null)
        {
            return Result<ProjectRevenueResponse>.Failure(
                Error.NotFound("projectrevenues.not_found", "Project revenue not found."));
        }

        return Result<ProjectRevenueResponse>.Success(MapResponse(revenue));
    }

    public async Task<Result<ProjectRevenueResponse>> CreateAsync(
        CreateProjectRevenueRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateMutationAsync(
            request.ProjectId,
            request.Month,
            request.Year,
            request.ValueIgaming,
            request.ValueVendas,
            request.GroupPercentage,
            request.Notes,
            excludeId: null,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<ProjectRevenueResponse>.Failure(validation.Error!);
        }

        var revenue = new ProjectRevenue
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Month = request.Month,
            Year = request.Year,
            ValueIgaming = request.ValueIgaming,
            ValueVendas = request.ValueVendas,
            Value = request.ValueIgaming + request.ValueVendas,
            GroupPercentage = request.GroupPercentage,
            Notes = NormalizeOptionalText(request.Notes)
        };

        _dbContext.ProjectRevenues.Add(revenue);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(revenue).Reference(r => r.Project).LoadAsync(cancellationToken);

        return Result<ProjectRevenueResponse>.Success(MapResponse(revenue));
    }

    public async Task<Result<ProjectRevenueResponse>> UpdateAsync(
        Guid projectRevenueId,
        UpdateProjectRevenueRequest request,
        CancellationToken cancellationToken = default)
    {
        var revenue = await _dbContext.ProjectRevenues
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == projectRevenueId, cancellationToken);

        if (revenue is null)
        {
            return Result<ProjectRevenueResponse>.Failure(
                Error.NotFound("projectrevenues.not_found", "Project revenue not found."));
        }

        var validation = await ValidateMutationAsync(
            request.ProjectId,
            request.Month,
            request.Year,
            request.ValueIgaming,
            request.ValueVendas,
            request.GroupPercentage,
            request.Notes,
            excludeId: projectRevenueId,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<ProjectRevenueResponse>.Failure(validation.Error!);
        }

        revenue.ProjectId = request.ProjectId;
        revenue.Month = request.Month;
        revenue.Year = request.Year;
        revenue.ValueIgaming = request.ValueIgaming;
        revenue.ValueVendas = request.ValueVendas;
        revenue.Value = request.ValueIgaming + request.ValueVendas;
        revenue.GroupPercentage = request.GroupPercentage;
        revenue.Notes = NormalizeOptionalText(request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(revenue).Reference(r => r.Project).LoadAsync(cancellationToken);

        return Result<ProjectRevenueResponse>.Success(MapResponse(revenue));
    }

    private static Result ValidateListFilters(ProjectRevenueListFilters filters)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateMutationAsync(
        Guid projectId,
        int month,
        int year,
        decimal valueIgaming,
        decimal valueVendas,
        decimal groupPercentage,
        string? notes,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.invalid_year", "Year is out of allowed range."));
        }

        if (valueIgaming < 0 || valueVendas < 0)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.invalid_values", "Revenue values cannot be negative."));
        }

        if (valueIgaming + valueVendas <= 0)
        {
            return Result.Failure(
                Error.Validation(
                    "projectrevenues.values_required",
                    "At least one revenue value must be greater than zero."));
        }

        if (groupPercentage is < 0 or > 100)
        {
            return Result.Failure(
                Error.Validation(
                    "projectrevenues.invalid_group_percentage",
                    "Group percentage must be between 0 and 100."));
        }

        if (notes is not null && notes.Length > NotesMaxLength)
        {
            return Result.Failure(
                Error.Validation("projectrevenues.notes_too_long", "Notes exceed maximum length."));
        }

        if (!await _dbContext.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            return Result.Failure(
                Error.Validation("projectrevenues.project_not_found", "Project not found."));
        }

        var duplicateQuery = _dbContext.ProjectRevenues
            .Where(r => r.ProjectId == projectId && r.Month == month && r.Year == year);

        if (excludeId is not null)
        {
            duplicateQuery = duplicateQuery.Where(r => r.Id != excludeId.Value);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            return Result.Failure(
                Error.Conflict(
                    "projectrevenues.duplicate",
                    "A revenue record already exists for this project and competence."));
        }

        return Result.Success();
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProjectRevenueResponse MapResponse(ProjectRevenue revenue) =>
        new(
            revenue.Id,
            revenue.ProjectId,
            revenue.Project.Name,
            revenue.Month,
            revenue.Year,
            revenue.ValueIgaming,
            revenue.ValueVendas,
            revenue.Value,
            revenue.GroupPercentage,
            revenue.Notes);
}
