using BuildingBlocks.Results;
using Core.Application.MasterData;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.MasterData;

public sealed class MasterDataStore : IMasterDataStore
{
    private readonly AppDbContext _dbContext;
    private readonly MasterDataCache _masterDataCache;

    public MasterDataStore(AppDbContext dbContext, MasterDataCache masterDataCache)
    {
        _dbContext = dbContext;
        _masterDataCache = masterDataCache;
    }

    private async Task SaveChangesAndInvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        _masterDataCache.InvalidateAll();
    }

    public async Task<Result<IReadOnlyList<DepartmentResponse>>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => MapDepartment(d))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DepartmentResponse>>.Success(departments);
    }

    public async Task<Result<DepartmentResponse>> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId, cancellationToken);

        if (department is null)
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound("departments.not_found", "Department not found."));
        }

        return Result<DepartmentResponse>.Success(MapDepartment(department));
    }

    public async Task<Result<DepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateDepartmentRequest(request.Name, request.LowRevenueThreshold, request.LowRevenueBonusPct, request.GoalBonusPercentage);
        if (validation.IsFailure)
        {
            return Result<DepartmentResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.Departments.AnyAsync(d => d.Name == normalizedName, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict("departments.duplicate", "A department with this name already exists."));
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CalculationType = request.CalculationType,
            GoalBonusPercentage = request.GoalBonusPercentage,
            LowRevenueThreshold = request.LowRevenueThreshold,
            LowRevenueBonusPct = request.LowRevenueBonusPct,
            Description = NormalizeOptionalText(request.Description),
            IsActive = request.IsActive,
            IsAllocatedFixed = request.IsAllocatedFixed,
            RoutesFixedToLimaKarttos = request.RoutesFixedToLimaKarttos
        };

        _dbContext.Departments.Add(department);
        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<DepartmentResponse>.Success(MapDepartment(department));
    }

    public async Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound("departments.not_found", "Department not found."));
        }

        var validation = ValidateDepartmentRequest(request.Name, request.LowRevenueThreshold, request.LowRevenueBonusPct, request.GoalBonusPercentage);
        if (validation.IsFailure)
        {
            return Result<DepartmentResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.Departments.AnyAsync(d => d.Name == normalizedName && d.Id != departmentId, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict("departments.duplicate", "A department with this name already exists."));
        }

        department.Name = normalizedName;
        department.CalculationType = request.CalculationType;
        department.GoalBonusPercentage = request.GoalBonusPercentage;
        department.LowRevenueThreshold = request.LowRevenueThreshold;
        department.LowRevenueBonusPct = request.LowRevenueBonusPct;
        department.Description = NormalizeOptionalText(request.Description);
        department.IsActive = request.IsActive;
        department.IsAllocatedFixed = request.IsAllocatedFixed;
        department.RoutesFixedToLimaKarttos = request.RoutesFixedToLimaKarttos;

        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<DepartmentResponse>.Success(MapDepartment(department));
    }

    public async Task<Result<IReadOnlyList<CareerLevelResponse>>> GetCareerLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _dbContext.CareerLevels
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => MapCareerLevel(c))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CareerLevelResponse>>.Success(levels);
    }

    public async Task<Result<CareerLevelResponse>> GetCareerLevelByIdAsync(Guid careerLevelId, CancellationToken cancellationToken = default)
    {
        var level = await _dbContext.CareerLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == careerLevelId, cancellationToken);

        if (level is null)
        {
            return Result<CareerLevelResponse>.Failure(Error.NotFound("careerlevels.not_found", "Career level not found."));
        }

        return Result<CareerLevelResponse>.Success(MapCareerLevel(level));
    }

    public async Task<Result<CareerLevelResponse>> CreateCareerLevelAsync(CreateCareerLevelRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCareerLevelRequestAsync(request.Name, request.DepartmentId, request.FtdBonusEvery, cancellationToken);
        if (validation.IsFailure)
        {
            return Result<CareerLevelResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.CareerLevels.AnyAsync(c => c.DepartmentId == request.DepartmentId && c.Name == normalizedName, cancellationToken))
        {
            return Result<CareerLevelResponse>.Failure(Error.Conflict("careerlevels.duplicate", "A career level with this name already exists for the department."));
        }

        var level = new CareerLevel { Id = Guid.NewGuid() };
        ApplyCareerLevelRequest(level, request);
        level.Name = normalizedName;

        _dbContext.CareerLevels.Add(level);
        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<CareerLevelResponse>.Success(MapCareerLevel(level));
    }

    public async Task<Result<CareerLevelResponse>> UpdateCareerLevelAsync(Guid careerLevelId, UpdateCareerLevelRequest request, CancellationToken cancellationToken = default)
    {
        var level = await _dbContext.CareerLevels.FirstOrDefaultAsync(c => c.Id == careerLevelId, cancellationToken);
        if (level is null)
        {
            return Result<CareerLevelResponse>.Failure(Error.NotFound("careerlevels.not_found", "Career level not found."));
        }

        var validation = await ValidateCareerLevelRequestAsync(request.Name, request.DepartmentId, request.FtdBonusEvery, cancellationToken);
        if (validation.IsFailure)
        {
            return Result<CareerLevelResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.CareerLevels.AnyAsync(
                c => c.DepartmentId == request.DepartmentId && c.Name == normalizedName && c.Id != careerLevelId,
                cancellationToken))
        {
            return Result<CareerLevelResponse>.Failure(Error.Conflict("careerlevels.duplicate", "A career level with this name already exists for the department."));
        }

        level.Name = normalizedName;
        ApplyCareerLevelRequest(level, request);

        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<CareerLevelResponse>.Success(MapCareerLevel(level));
    }

    public async Task<Result<IReadOnlyList<ProjectResponse>>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => MapProject(p))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProjectResponse>>.Success(projects);
    }

    public async Task<Result<ProjectResponse>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            return Result<ProjectResponse>.Failure(Error.NotFound("projects.not_found", "Project not found."));
        }

        return Result<ProjectResponse>.Success(MapProject(project));
    }

    public async Task<Result<ProjectResponse>> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateProjectRequest(request.Name);
        if (validation.IsFailure)
        {
            return Result<ProjectResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.Projects.AnyAsync(p => p.Name == normalizedName, cancellationToken))
        {
            return Result<ProjectResponse>.Failure(Error.Conflict("projects.duplicate", "A project with this name already exists."));
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Client = NormalizeOptionalText(request.Client),
            Platform = request.Platform,
            IsActive = request.IsActive,
            IsDefaultAllocationTarget = request.IsDefaultAllocationTarget,
            ExcludesGoalBonus = request.ExcludesGoalBonus,
            ExcludesSupervisorFixedAllocation = request.ExcludesSupervisorFixedAllocation
        };

        _dbContext.Projects.Add(project);
        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<ProjectResponse>.Success(MapProject(project));
    }

    public async Task<Result<ProjectResponse>> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return Result<ProjectResponse>.Failure(Error.NotFound("projects.not_found", "Project not found."));
        }

        var validation = ValidateProjectRequest(request.Name);
        if (validation.IsFailure)
        {
            return Result<ProjectResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.Projects.AnyAsync(p => p.Name == normalizedName && p.Id != projectId, cancellationToken))
        {
            return Result<ProjectResponse>.Failure(Error.Conflict("projects.duplicate", "A project with this name already exists."));
        }

        project.Name = normalizedName;
        project.Client = NormalizeOptionalText(request.Client);
        project.Platform = request.Platform;
        project.IsActive = request.IsActive;
        project.IsDefaultAllocationTarget = request.IsDefaultAllocationTarget;
        project.ExcludesGoalBonus = request.ExcludesGoalBonus;
        project.ExcludesSupervisorFixedAllocation = request.ExcludesSupervisorFixedAllocation;

        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<ProjectResponse>.Success(MapProject(project));
    }

    public async Task<Result<IReadOnlyList<PaymentMethodResponse>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        var methods = await _dbContext.PaymentMethods
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => MapPaymentMethod(p))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PaymentMethodResponse>>.Success(methods);
    }

    public async Task<Result<PaymentMethodResponse>> GetPaymentMethodByIdAsync(Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentMethodId, cancellationToken);

        if (method is null)
        {
            return Result<PaymentMethodResponse>.Failure(Error.NotFound("paymentmethods.not_found", "Payment method not found."));
        }

        return Result<PaymentMethodResponse>.Success(MapPaymentMethod(method));
    }

    public async Task<Result<PaymentMethodResponse>> CreatePaymentMethodAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePaymentMethodRequest(request.Name);
        if (validation.IsFailure)
        {
            return Result<PaymentMethodResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.PaymentMethods.AnyAsync(p => p.Name == normalizedName, cancellationToken))
        {
            return Result<PaymentMethodResponse>.Failure(Error.Conflict("paymentmethods.duplicate", "A payment method with this name already exists."));
        }

        var method = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            IsActive = request.IsActive
        };

        _dbContext.PaymentMethods.Add(method);
        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<PaymentMethodResponse>.Success(MapPaymentMethod(method));
    }

    public async Task<Result<PaymentMethodResponse>> UpdatePaymentMethodAsync(Guid paymentMethodId, UpdatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == paymentMethodId, cancellationToken);
        if (method is null)
        {
            return Result<PaymentMethodResponse>.Failure(Error.NotFound("paymentmethods.not_found", "Payment method not found."));
        }

        var validation = ValidatePaymentMethodRequest(request.Name);
        if (validation.IsFailure)
        {
            return Result<PaymentMethodResponse>.Failure(validation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _dbContext.PaymentMethods.AnyAsync(p => p.Name == normalizedName && p.Id != paymentMethodId, cancellationToken))
        {
            return Result<PaymentMethodResponse>.Failure(Error.Conflict("paymentmethods.duplicate", "A payment method with this name already exists."));
        }

        method.Name = normalizedName;
        method.IsActive = request.IsActive;

        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result<PaymentMethodResponse>.Success(MapPaymentMethod(method));
    }

    public async Task<Result> DeletePaymentMethodAsync(Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == paymentMethodId, cancellationToken);
        if (method is null)
        {
            return Result.Failure(Error.NotFound("paymentmethods.not_found", "Payment method not found."));
        }

        if (await _dbContext.ProjectCosts.AnyAsync(c => c.PaymentMethodId == paymentMethodId, cancellationToken))
        {
            return Result.Failure(
                Error.Conflict("paymentmethods.in_use", "Payment method is referenced by cashflow entries."));
        }

        _dbContext.PaymentMethods.Remove(method);
        await SaveChangesAndInvalidateCacheAsync(cancellationToken);

        return Result.Success();
    }

    private static Result ValidateDepartmentRequest(string name, decimal lowRevenueThreshold, decimal lowRevenueBonusPct, decimal goalBonusPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("departments.name_required", "Name is required."));
        }

        if (lowRevenueThreshold < 0 || lowRevenueBonusPct < 0 || goalBonusPercentage < 0)
        {
            return Result.Failure(Error.Validation("departments.invalid_values", "Numeric values cannot be negative."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateCareerLevelRequestAsync(string name, Guid? departmentId, int ftdBonusEvery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("careerlevels.name_required", "Name is required."));
        }

        if (ftdBonusEvery < 0)
        {
            return Result.Failure(Error.Validation("careerlevels.invalid_values", "FtdBonusEvery cannot be negative."));
        }

        if (departmentId is not null &&
            !await _dbContext.Departments.AnyAsync(d => d.Id == departmentId, cancellationToken))
        {
            return Result.Failure(Error.Validation("careerlevels.department_not_found", "Department not found."));
        }

        return Result.Success();
    }

    private static Result ValidateProjectRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("projects.name_required", "Name is required."));
        }

        return Result.Success();
    }

    private static Result ValidatePaymentMethodRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("paymentmethods.name_required", "Name is required."));
        }

        return Result.Success();
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DepartmentResponse MapDepartment(Department department) =>
        new(
            department.Id,
            department.Name,
            department.CalculationType,
            department.GoalBonusPercentage,
            department.LowRevenueThreshold,
            department.LowRevenueBonusPct,
            department.Description,
            department.IsActive,
            department.IsAllocatedFixed,
            department.RoutesFixedToLimaKarttos);

    private static CareerLevelResponse MapCareerLevel(CareerLevel level) =>
        new(
            level.Id,
            level.Name,
            level.DepartmentId,
            level.Profile,
            level.IsActive,
            level.BaseSalary,
            level.CommissionWithoutGoalPct,
            level.CommissionWithGoalPct,
            level.CommissionWithSuperGoalPct,
            level.GroupCommissionPerPercent,
            level.GroupCommissionPer20Percent,
            level.DefaultCpaValue,
            level.GoalBonusValue,
            level.FtdRateBase,
            level.FtdRateWithGoal,
            level.FtdRateWithSuperGoal,
            level.FtdSuperbetRate,
            level.FtdBonusEvery,
            level.FtdBonusValue,
            level.SalesPctBase,
            level.SalesPctWithGoal,
            level.SalesPctWithSuperGoal,
            level.SalesBonusEvery,
            level.SalesBonusValue,
            level.RevPct,
            level.BetanoInternaValue,
            level.BetanoMundoBetValue,
            level.SupFtdSuperbetNoGoal,
            level.SupFtdSuperbetWithGoal,
            level.SupFtdOtherNoGoal,
            level.SupFtdOtherWithGoal,
            level.SupSalesPctNoGoal,
            level.SupSalesPctWithGoal,
            level.SupRevPct,
            level.NetRevenueFactor,
            level.NetRevenuePctNoGoal,
            level.NetRevenuePctWithGoal,
            level.TrafficInvestmentCommissionPct,
            level.TrafficCpaEsportiva,
            level.TrafficCpaStake,
            level.TrafficCpaBetano,
            level.TrafficCpaBetMgm,
            level.TrafficCpaNovibet,
            level.TrafficCpaBetFair,
            level.TrafficCpaBlaze,
            level.TrafficCpaSuperbet,
            level.TrafficCpaHiperbet,
            level.TrafficSupBonus,
            level.TrafficSupCommissionPct);

    private static ProjectResponse MapProject(Project project) =>
        new(
            project.Id,
            project.Name,
            project.Client,
            project.Platform,
            project.IsActive,
            project.IsDefaultAllocationTarget,
            project.ExcludesGoalBonus,
            project.ExcludesSupervisorFixedAllocation);

    private static PaymentMethodResponse MapPaymentMethod(PaymentMethod method) =>
        new(method.Id, method.Name, method.IsActive);

    private static void ApplyCareerLevelRequest(CareerLevel level, CreateCareerLevelRequest request)
    {
        level.DepartmentId = request.DepartmentId;
        level.Profile = request.Profile;
        level.IsActive = request.IsActive;
        level.BaseSalary = request.BaseSalary;
        level.CommissionWithoutGoalPct = request.CommissionWithoutGoalPct;
        level.CommissionWithGoalPct = request.CommissionWithGoalPct;
        level.CommissionWithSuperGoalPct = request.CommissionWithSuperGoalPct;
        level.GroupCommissionPerPercent = request.GroupCommissionPerPercent;
        level.GroupCommissionPer20Percent = request.GroupCommissionPer20Percent;
        level.DefaultCpaValue = request.DefaultCpaValue;
        level.GoalBonusValue = request.GoalBonusValue;
        level.FtdRateBase = request.FtdRateBase;
        level.FtdRateWithGoal = request.FtdRateWithGoal;
        level.FtdRateWithSuperGoal = request.FtdRateWithSuperGoal;
        level.FtdSuperbetRate = request.FtdSuperbetRate;
        level.FtdBonusEvery = request.FtdBonusEvery;
        level.FtdBonusValue = request.FtdBonusValue;
        level.SalesPctBase = request.SalesPctBase;
        level.SalesPctWithGoal = request.SalesPctWithGoal;
        level.SalesPctWithSuperGoal = request.SalesPctWithSuperGoal;
        level.SalesBonusEvery = request.SalesBonusEvery;
        level.SalesBonusValue = request.SalesBonusValue;
        level.RevPct = request.RevPct;
        level.BetanoInternaValue = request.BetanoInternaValue;
        level.BetanoMundoBetValue = request.BetanoMundoBetValue;
        level.SupFtdSuperbetNoGoal = request.SupFtdSuperbetNoGoal;
        level.SupFtdSuperbetWithGoal = request.SupFtdSuperbetWithGoal;
        level.SupFtdOtherNoGoal = request.SupFtdOtherNoGoal;
        level.SupFtdOtherWithGoal = request.SupFtdOtherWithGoal;
        level.SupSalesPctNoGoal = request.SupSalesPctNoGoal;
        level.SupSalesPctWithGoal = request.SupSalesPctWithGoal;
        level.SupRevPct = request.SupRevPct;
        level.NetRevenueFactor = request.NetRevenueFactor;
        level.NetRevenuePctNoGoal = request.NetRevenuePctNoGoal;
        level.NetRevenuePctWithGoal = request.NetRevenuePctWithGoal;
        level.TrafficInvestmentCommissionPct = request.TrafficInvestmentCommissionPct;
        level.TrafficCpaEsportiva = request.TrafficCpaEsportiva;
        level.TrafficCpaStake = request.TrafficCpaStake;
        level.TrafficCpaBetano = request.TrafficCpaBetano;
        level.TrafficCpaBetMgm = request.TrafficCpaBetMgm;
        level.TrafficCpaNovibet = request.TrafficCpaNovibet;
        level.TrafficCpaBetFair = request.TrafficCpaBetFair;
        level.TrafficCpaBlaze = request.TrafficCpaBlaze;
        level.TrafficCpaSuperbet = request.TrafficCpaSuperbet;
        level.TrafficCpaHiperbet = request.TrafficCpaHiperbet;
        level.TrafficSupBonus = request.TrafficSupBonus;
        level.TrafficSupCommissionPct = request.TrafficSupCommissionPct;
    }

    private static void ApplyCareerLevelRequest(CareerLevel level, UpdateCareerLevelRequest request)
    {
        level.DepartmentId = request.DepartmentId;
        level.Profile = request.Profile;
        level.IsActive = request.IsActive;
        level.BaseSalary = request.BaseSalary;
        level.CommissionWithoutGoalPct = request.CommissionWithoutGoalPct;
        level.CommissionWithGoalPct = request.CommissionWithGoalPct;
        level.CommissionWithSuperGoalPct = request.CommissionWithSuperGoalPct;
        level.GroupCommissionPerPercent = request.GroupCommissionPerPercent;
        level.GroupCommissionPer20Percent = request.GroupCommissionPer20Percent;
        level.DefaultCpaValue = request.DefaultCpaValue;
        level.GoalBonusValue = request.GoalBonusValue;
        level.FtdRateBase = request.FtdRateBase;
        level.FtdRateWithGoal = request.FtdRateWithGoal;
        level.FtdRateWithSuperGoal = request.FtdRateWithSuperGoal;
        level.FtdSuperbetRate = request.FtdSuperbetRate;
        level.FtdBonusEvery = request.FtdBonusEvery;
        level.FtdBonusValue = request.FtdBonusValue;
        level.SalesPctBase = request.SalesPctBase;
        level.SalesPctWithGoal = request.SalesPctWithGoal;
        level.SalesPctWithSuperGoal = request.SalesPctWithSuperGoal;
        level.SalesBonusEvery = request.SalesBonusEvery;
        level.SalesBonusValue = request.SalesBonusValue;
        level.RevPct = request.RevPct;
        level.BetanoInternaValue = request.BetanoInternaValue;
        level.BetanoMundoBetValue = request.BetanoMundoBetValue;
        level.SupFtdSuperbetNoGoal = request.SupFtdSuperbetNoGoal;
        level.SupFtdSuperbetWithGoal = request.SupFtdSuperbetWithGoal;
        level.SupFtdOtherNoGoal = request.SupFtdOtherNoGoal;
        level.SupFtdOtherWithGoal = request.SupFtdOtherWithGoal;
        level.SupSalesPctNoGoal = request.SupSalesPctNoGoal;
        level.SupSalesPctWithGoal = request.SupSalesPctWithGoal;
        level.SupRevPct = request.SupRevPct;
        level.NetRevenueFactor = request.NetRevenueFactor;
        level.NetRevenuePctNoGoal = request.NetRevenuePctNoGoal;
        level.NetRevenuePctWithGoal = request.NetRevenuePctWithGoal;
        level.TrafficInvestmentCommissionPct = request.TrafficInvestmentCommissionPct;
        level.TrafficCpaEsportiva = request.TrafficCpaEsportiva;
        level.TrafficCpaStake = request.TrafficCpaStake;
        level.TrafficCpaBetano = request.TrafficCpaBetano;
        level.TrafficCpaBetMgm = request.TrafficCpaBetMgm;
        level.TrafficCpaNovibet = request.TrafficCpaNovibet;
        level.TrafficCpaBetFair = request.TrafficCpaBetFair;
        level.TrafficCpaBlaze = request.TrafficCpaBlaze;
        level.TrafficCpaSuperbet = request.TrafficCpaSuperbet;
        level.TrafficCpaHiperbet = request.TrafficCpaHiperbet;
        level.TrafficSupBonus = request.TrafficSupBonus;
        level.TrafficSupCommissionPct = request.TrafficSupCommissionPct;
    }
}
