using BuildingBlocks.Pagination;
using BuildingBlocks.Results;
using Core.Application.Collaborators;
using Core.Domain;
using Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Collaborators;

public sealed class CollaboratorStore : ICollaboratorStore
{
    private readonly AppDbContext _dbContext;

    public CollaboratorStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CollaboratorsListResponse>> GetCollaboratorsAsync(
        Guid? departmentId,
        string? search,
        bool? isActive,
        CollaboratorAccessContext access,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var scopeResult = ResolveDepartmentScope(departmentId, access);
        if (scopeResult.IsFailure)
        {
            return Result<CollaboratorsListResponse>.Failure(scopeResult.Error!);
        }

        var query = BuildQuery(scopeResult.Value!, search, isActive);
        var totalCount = await query.CountAsync(cancellationToken);
        var (responsePage, responsePageSize, skip) = Pagination.Normalize(page, pageSize);

        var collaborators = await query
            .OrderBy(collaborator => collaborator.Name)
            .Skip(skip)
            .Take(responsePageSize)
            .Select(collaborator => MapCollaborator(collaborator))
            .ToListAsync(cancellationToken);

        return Result<CollaboratorsListResponse>.Success(
            new CollaboratorsListResponse(
                collaborators,
                totalCount,
                responsePage,
                responsePageSize));
    }

    public async Task<Result<CollaboratorResponse>> GetCollaboratorByIdAsync(
        Guid collaboratorId,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await _dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.CareerLevel)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return Result<CollaboratorResponse>.Failure(
                Error.NotFound("collaborators.not_found", "Collaborator not found."));
        }

        if (!CanAccessDepartment(collaborator.DepartmentId, access))
        {
            return Result<CollaboratorResponse>.Failure(
                Error.Forbidden("collaborators.department_forbidden", "Collaborator is outside your department scope."));
        }

        return Result<CollaboratorResponse>.Success(MapCollaborator(collaborator));
    }

    public async Task<Result<CollaboratorResponse>> CreateCollaboratorAsync(
        CreateCollaboratorRequest request,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessDepartment(request.DepartmentId, access))
        {
            return Result<CollaboratorResponse>.Failure(
                Error.Forbidden("collaborators.department_forbidden", "Department is outside your scope."));
        }

        var validation = await ValidateCollaboratorRequestAsync(
            request.Name,
            request.DepartmentId,
            request.CareerLevelId,
            request.AdmissionDate,
            request.DismissalDate,
            request.BaseSalary,
            request.IsActive,
            request.PhotoUrl,
            cancellationToken);
        if (validation.IsFailure)
        {
            return Result<CollaboratorResponse>.Failure(validation.Error!);
        }

        var collaborator = new Collaborator { Id = Guid.NewGuid() };
        ApplyCollaboratorRequest(collaborator, request, validation.Value!.PhotoUrl);

        _dbContext.Collaborators.Add(collaborator);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await LoadCollaboratorWithRelationsAsync(collaborator.Id, cancellationToken);
        return Result<CollaboratorResponse>.Success(MapCollaborator(created!));
    }

    public async Task<Result<CollaboratorResponse>> UpdateCollaboratorAsync(
        Guid collaboratorId,
        UpdateCollaboratorRequest request,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await _dbContext.Collaborators
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return Result<CollaboratorResponse>.Failure(
                Error.NotFound("collaborators.not_found", "Collaborator not found."));
        }

        if (!CanAccessDepartment(collaborator.DepartmentId, access))
        {
            return Result<CollaboratorResponse>.Failure(
                Error.Forbidden("collaborators.department_forbidden", "Collaborator is outside your department scope."));
        }

        if (!CanAccessDepartment(request.DepartmentId, access))
        {
            return Result<CollaboratorResponse>.Failure(
                Error.Forbidden("collaborators.department_forbidden", "Department is outside your scope."));
        }

        var validation = await ValidateCollaboratorRequestAsync(
            request.Name,
            request.DepartmentId,
            request.CareerLevelId,
            request.AdmissionDate,
            request.DismissalDate,
            request.BaseSalary,
            request.IsActive,
            request.PhotoUrl,
            cancellationToken);
        if (validation.IsFailure)
        {
            return Result<CollaboratorResponse>.Failure(validation.Error!);
        }

        ApplyCollaboratorRequest(collaborator, request, validation.Value!.PhotoUrl);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await LoadCollaboratorWithRelationsAsync(collaborator.Id, cancellationToken);
        return Result<CollaboratorResponse>.Success(MapCollaborator(updated!));
    }

    private IQueryable<Collaborator> BuildQuery(
        DepartmentScope scope,
        string? search,
        bool? isActive)
    {
        var query = _dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.CareerLevel)
            .AsQueryable();

        if (scope.DepartmentId is not null)
        {
            query = query.Where(c => c.DepartmentId == scope.DepartmentId.Value);
        }
        else if (scope.AllowedDepartmentIds is not null)
        {
            query = query.Where(c => scope.AllowedDepartmentIds.Contains(c.DepartmentId));
        }

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.Name, $"%{term}%")
                || (c.JobTitle != null && EF.Functions.Like(c.JobTitle, $"%{term}%"))
                || (c.CareerLevel != null && EF.Functions.Like(c.CareerLevel.Name, $"%{term}%")));
        }

        return query;
    }

    private static Result<DepartmentScope> ResolveDepartmentScope(
        Guid? departmentId,
        CollaboratorAccessContext access)
    {
        if (!CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(departmentId, null));
        }

        var allowed = access.AllowedDepartmentIds ?? [];
        if (allowed.Count == 0)
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(null, []));
        }

        if (departmentId is not null && !allowed.Contains(departmentId.Value))
        {
            return Result<DepartmentScope>.Failure(
                Error.Forbidden("collaborators.department_forbidden", "Department is outside your scope."));
        }

        return Result<DepartmentScope>.Success(
            departmentId is not null
                ? new DepartmentScope(departmentId, null)
                : new DepartmentScope(null, allowed));
    }

    private static bool CanAccessDepartment(Guid departmentId, CollaboratorAccessContext access)
    {
        if (!CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            return true;
        }

        return access.AllowedDepartmentIds?.Contains(departmentId) == true;
    }

    private static CollaboratorResponse MapCollaborator(Collaborator collaborator) =>
        new(
            collaborator.Id,
            collaborator.Name,
            collaborator.DepartmentId,
            collaborator.Department.Name,
            collaborator.CareerLevelId,
            collaborator.CareerLevel?.Name,
            collaborator.JobTitle,
            collaborator.AdmissionDate,
            collaborator.DismissalDate,
            collaborator.PixKey,
            collaborator.BaseSalary,
            collaborator.Email,
            collaborator.PhotoUrl,
            collaborator.IsActive,
            collaborator.CalculationProfileOverride);

    private async Task<Collaborator?> LoadCollaboratorWithRelationsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken) =>
        await _dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.CareerLevel)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

    private sealed record ValidatedCollaboratorInput(string? PhotoUrl);

    private async Task<Result<ValidatedCollaboratorInput>> ValidateCollaboratorRequestAsync(
        string name,
        Guid departmentId,
        Guid? careerLevelId,
        DateOnly? admissionDate,
        DateOnly? dismissalDate,
        decimal? baseSalary,
        bool isActive,
        string? photoUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.name_required", "Name is required."));
        }

        if (departmentId == Guid.Empty)
        {
            return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.department_required", "Department is required."));
        }

        if (!await _dbContext.Departments.AnyAsync(d => d.Id == departmentId, cancellationToken))
        {
            return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.department_not_found", "Department not found."));
        }

        if (careerLevelId is not null)
        {
            var careerLevel = await _dbContext.CareerLevels
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == careerLevelId.Value, cancellationToken);

            if (careerLevel is null)
            {
                return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.careerlevel_not_found", "Career level not found."));
            }

            if (careerLevel.DepartmentId is not null && careerLevel.DepartmentId != departmentId)
            {
                return Result<ValidatedCollaboratorInput>.Failure(
                    Error.Validation(
                        "collaborators.careerlevel_department_mismatch",
                        "Career level does not belong to the selected department."));
            }
        }

        if (baseSalary is < 0)
        {
            return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.invalid_values", "Base salary cannot be negative."));
        }

        if (admissionDate is not null && dismissalDate is not null && dismissalDate < admissionDate)
        {
            return Result<ValidatedCollaboratorInput>.Failure(Error.Validation("collaborators.invalid_dates", "Dismissal date cannot be before admission date."));
        }

        if (!isActive && dismissalDate is null)
        {
            return Result<ValidatedCollaboratorInput>.Failure(
                Error.Validation("collaborators.dismissal_date_required", "Dismissal date is required when collaborator is inactive."));
        }

        var photoUrlValidation = SafeExternalUrlValidator.Validate(photoUrl);
        if (photoUrlValidation.IsFailure)
        {
            return Result<ValidatedCollaboratorInput>.Failure(photoUrlValidation.Error!);
        }

        return Result<ValidatedCollaboratorInput>.Success(new ValidatedCollaboratorInput(photoUrlValidation.Value));
    }

    private static void ApplyCollaboratorRequest(
        Collaborator collaborator,
        CreateCollaboratorRequest request,
        string? validatedPhotoUrl)
    {
        collaborator.Name = request.Name.Trim();
        collaborator.DepartmentId = request.DepartmentId;
        collaborator.CareerLevelId = request.CareerLevelId;
        collaborator.JobTitle = NormalizeOptionalText(request.JobTitle);
        collaborator.AdmissionDate = request.AdmissionDate;
        collaborator.PixKey = NormalizeOptionalText(request.PixKey);
        collaborator.BaseSalary = request.BaseSalary;
        collaborator.Email = NormalizeOptionalText(request.Email);
        collaborator.PhotoUrl = validatedPhotoUrl;
        collaborator.IsActive = request.IsActive;
        collaborator.CalculationProfileOverride = request.CalculationProfileOverride;
        collaborator.DismissalDate = request.IsActive ? null : request.DismissalDate;
    }

    private static void ApplyCollaboratorRequest(
        Collaborator collaborator,
        UpdateCollaboratorRequest request,
        string? validatedPhotoUrl)
    {
        collaborator.Name = request.Name.Trim();
        collaborator.DepartmentId = request.DepartmentId;
        collaborator.CareerLevelId = request.CareerLevelId;
        collaborator.JobTitle = NormalizeOptionalText(request.JobTitle);
        collaborator.AdmissionDate = request.AdmissionDate;
        collaborator.PixKey = NormalizeOptionalText(request.PixKey);
        collaborator.BaseSalary = request.BaseSalary;
        collaborator.Email = NormalizeOptionalText(request.Email);
        collaborator.PhotoUrl = validatedPhotoUrl;
        collaborator.IsActive = request.IsActive;
        collaborator.CalculationProfileOverride = request.CalculationProfileOverride;
        collaborator.DismissalDate = request.IsActive ? null : request.DismissalDate;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record DepartmentScope(Guid? DepartmentId, IReadOnlyList<Guid>? AllowedDepartmentIds);
}
