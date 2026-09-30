using BuildingBlocks.Results;
using Core.Application.Traffic;
using Core.Domain;
using Core.Domain.TrafficInvestmentCalculation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Traffic;

public sealed class TrafficInvestmentStore : ITrafficInvestmentStore
{
    private const int NotesMaxLength = 2048;

    private readonly AppDbContext _dbContext;

    public TrafficInvestmentStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TrafficInvestmentListItemResponse>>> GetListAsync(
        TrafficInvestmentListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateInvestmentListFilters(filters);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<TrafficInvestmentListItemResponse>>.Failure(validation.Error!);
        }

        var query = BuildInvestmentQuery(filters);

        var investments = await query
            .OrderByDescending(investment => investment.Year)
            .ThenByDescending(investment => investment.Month)
            .ThenBy(investment => investment.Project.Name)
            .ToListAsync(cancellationToken);

        var items = new List<TrafficInvestmentListItemResponse>(investments.Count);
        foreach (var investment in investments)
        {
            var calculation = CalculateInvestment(investment);
            if (calculation.IsFailure)
            {
                return Result<IReadOnlyList<TrafficInvestmentListItemResponse>>.Failure(calculation.Error!);
            }

            items.Add(TrafficInvestmentMapper.MapListItem(investment, calculation.Value));
        }

        return Result<IReadOnlyList<TrafficInvestmentListItemResponse>>.Success(items);
    }

    public async Task<Result<TrafficInvestmentResponse>> GetByIdAsync(
        Guid trafficInvestmentId,
        CancellationToken cancellationToken = default)
    {
        var investment = await LoadInvestmentGraphAsync(trafficInvestmentId, cancellationToken);
        if (investment is null)
        {
            return Result<TrafficInvestmentResponse>.Failure(
                Error.NotFound("trafficinvestments.not_found", "Traffic investment not found."));
        }

        return MapDetail(investment);
    }

    public async Task<Result<TrafficInvestmentResponse>> CreateAsync(
        CreateTrafficInvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateInvestmentMutationAsync(
            request.ProjectId,
            request.Month,
            request.Year,
            request.MonthlyTarget,
            request.Weeks,
            excludeId: null,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<TrafficInvestmentResponse>.Failure(validation.Error!);
        }

        var normalizedWeeks = TrafficInvestmentMapper.NormalizeWeekRequests(request.Weeks);
        var investment = new TrafficInvestment
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Month = request.Month,
            Year = request.Year,
            MonthlyTarget = request.MonthlyTarget
        };

        TrafficInvestmentMapper.ApplyWeeks(investment, normalizedWeeks);

        var calculation = CalculateInvestment(investment);
        if (calculation.IsFailure)
        {
            return Result<TrafficInvestmentResponse>.Failure(calculation.Error!);
        }

        _dbContext.TrafficInvestments.Add(investment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(investment).Reference(entity => entity.Project).LoadAsync(cancellationToken);

        return TrafficInvestmentMapper.MapDetail(investment, calculation.Value);
    }

    public async Task<Result<TrafficInvestmentResponse>> UpdateAsync(
        Guid trafficInvestmentId,
        UpdateTrafficInvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var investment = await _dbContext.TrafficInvestments
            .Include(entity => entity.Project)
            .Include(entity => entity.Weeks)
            .ThenInclude(week => week.Deposits)
            .Include(entity => entity.Weeks)
            .ThenInclude(week => week.ChannelSpends)
            .FirstOrDefaultAsync(entity => entity.Id == trafficInvestmentId, cancellationToken);

        if (investment is null)
        {
            return Result<TrafficInvestmentResponse>.Failure(
                Error.NotFound("trafficinvestments.not_found", "Traffic investment not found."));
        }

        var validation = await ValidateInvestmentMutationAsync(
            request.ProjectId,
            request.Month,
            request.Year,
            request.MonthlyTarget,
            request.Weeks,
            excludeId: trafficInvestmentId,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<TrafficInvestmentResponse>.Failure(validation.Error!);
        }

        var normalizedWeeks = TrafficInvestmentMapper.NormalizeWeekRequests(request.Weeks);

        investment.ProjectId = request.ProjectId;
        investment.Month = request.Month;
        investment.Year = request.Year;
        investment.MonthlyTarget = request.MonthlyTarget;

        UpsertWeeks(investment, normalizedWeeks);

        var calculation = CalculateInvestment(investment);
        if (calculation.IsFailure)
        {
            return Result<TrafficInvestmentResponse>.Failure(calculation.Error!);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(investment).Reference(entity => entity.Project).LoadAsync(cancellationToken);

        return TrafficInvestmentMapper.MapDetail(investment, calculation.Value);
    }

    public async Task<Result<IReadOnlyList<TrafficProjectDepositResponse>>> GetProjectDepositsAsync(
        TrafficProjectDepositListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateProjectDepositListFilters(filters);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<TrafficProjectDepositResponse>>.Failure(validation.Error!);
        }

        var query = _dbContext.TrafficProjectDeposits
            .AsNoTracking()
            .Include(deposit => deposit.Project)
            .AsQueryable();

        if (filters.ProjectId is not null)
        {
            query = query.Where(deposit => deposit.ProjectId == filters.ProjectId.Value);
        }

        if (filters.Month is not null && filters.Year is not null)
        {
            var month = filters.Month.Value;
            var year = filters.Year.Value;
            query = query.Where(deposit =>
                deposit.DepositDate.Year == year && deposit.DepositDate.Month == month);
        }
        else if (filters.Year is not null)
        {
            query = query.Where(deposit => deposit.DepositDate.Year == filters.Year.Value);
        }

        var items = await query
            .OrderByDescending(deposit => deposit.DepositDate)
            .ThenBy(deposit => deposit.Project.Name)
            .Select(deposit => MapProjectDeposit(deposit))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TrafficProjectDepositResponse>>.Success(items);
    }

    public async Task<Result<TrafficProjectDepositResponse>> CreateProjectDepositAsync(
        CreateTrafficProjectDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0m)
        {
            return Result<TrafficProjectDepositResponse>.Failure(
                Error.Validation(
                    request.Amount < 0m ? "trafficdeposits.invalid_amount" : "trafficdeposits.amount_required",
                    request.Amount < 0m
                        ? "Deposit amount cannot be negative."
                        : "Deposit amount must be greater than zero."));
        }

        if (request.Notes is not null && request.Notes.Length > NotesMaxLength)
        {
            return Result<TrafficProjectDepositResponse>.Failure(
                Error.Validation("trafficdeposits.notes_too_long", "Notes exceed maximum length."));
        }

        if (!await _dbContext.Projects.AnyAsync(project => project.Id == request.ProjectId, cancellationToken))
        {
            return Result<TrafficProjectDepositResponse>.Failure(
                Error.Validation("trafficdeposits.project_not_found", "Project not found."));
        }

        var deposit = new TrafficProjectDeposit
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            DepositDate = request.DepositDate,
            Amount = request.Amount,
            Notes = NormalizeOptionalText(request.Notes)
        };

        _dbContext.TrafficProjectDeposits.Add(deposit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(deposit).Reference(entity => entity.Project).LoadAsync(cancellationToken);

        return Result<TrafficProjectDepositResponse>.Success(MapProjectDeposit(deposit));
    }

    private IQueryable<TrafficInvestment> BuildInvestmentQuery(TrafficInvestmentListFilters filters)
    {
        var query = _dbContext.TrafficInvestments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(investment => investment.Project)
            .Include(investment => investment.Weeks)
            .ThenInclude(week => week.Deposits)
            .Include(investment => investment.Weeks)
            .ThenInclude(week => week.ChannelSpends)
            .AsQueryable();

        if (filters.Month is not null)
        {
            query = query.Where(investment => investment.Month == filters.Month.Value);
        }

        if (filters.Year is not null)
        {
            query = query.Where(investment => investment.Year == filters.Year.Value);
        }

        if (filters.ProjectId is not null)
        {
            query = query.Where(investment => investment.ProjectId == filters.ProjectId.Value);
        }

        return query;
    }

    private async Task<TrafficInvestment?> LoadInvestmentGraphAsync(
        Guid trafficInvestmentId,
        CancellationToken cancellationToken) =>
        await _dbContext.TrafficInvestments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(investment => investment.Project)
            .Include(investment => investment.Weeks)
            .ThenInclude(week => week.Deposits)
            .Include(investment => investment.Weeks)
            .ThenInclude(week => week.ChannelSpends)
            .FirstOrDefaultAsync(investment => investment.Id == trafficInvestmentId, cancellationToken);

    private static Result<TrafficInvestmentResponse> MapDetail(TrafficInvestment investment)
    {
        var calculation = CalculateInvestment(investment);
        return calculation.IsFailure
            ? Result<TrafficInvestmentResponse>.Failure(calculation.Error!)
            : TrafficInvestmentMapper.MapDetail(investment, calculation.Value);
    }

    private static Result<TrafficInvestmentResult> CalculateInvestment(TrafficInvestment investment) =>
        TrafficInvestmentCalculator.Calculate(TrafficInvestmentMapper.ToCalculatorInput(investment));

    private async Task<Result> ValidateInvestmentMutationAsync(
        Guid projectId,
        int month,
        int year,
        decimal monthlyTarget,
        IReadOnlyList<TrafficWeekRequest> weeks,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.invalid_year", "Year is out of allowed range."));
        }

        if (monthlyTarget < 0m)
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.invalid_monthly_target", "Monthly target cannot be negative."));
        }

        foreach (var week in weeks)
        {
            if (week.WeekNumber is < 1 or > TrafficInvestmentConstants.TotalWeeks)
            {
                return Result.Failure(
                    Error.Validation(
                        "trafficinvestments.invalid_week_number",
                        $"Week number must be between 1 and {TrafficInvestmentConstants.TotalWeeks}."));
            }

            foreach (var deposit in week.Deposits)
            {
                if (deposit.RequestedAmount < 0m || deposit.DepositedAmount < 0m)
                {
                    return Result.Failure(
                        Error.Validation(
                            "traffic.investment.negative_deposit",
                            "Valores de depósito não podem ser negativos."));
                }
            }

            foreach (var spend in week.ChannelSpends)
            {
                if (spend.Amount < 0m)
                {
                    return Result.Failure(
                        Error.Validation(
                            "traffic.investment.negative_spend",
                            "Valores de gasto não podem ser negativos."));
                }
            }
        }

        if (weeks.GroupBy(week => week.WeekNumber).Any(group => group.Count() > 1))
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.duplicate_week", "Duplicate week numbers are not allowed."));
        }

        if (!await _dbContext.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.project_not_found", "Project not found."));
        }

        var duplicateQuery = _dbContext.TrafficInvestments
            .Where(investment => investment.ProjectId == projectId
                                 && investment.Month == month
                                 && investment.Year == year);

        if (excludeId is not null)
        {
            duplicateQuery = duplicateQuery.Where(investment => investment.Id != excludeId.Value);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            return Result.Failure(
                Error.Conflict(
                    "trafficinvestments.duplicate",
                    "A traffic investment already exists for this project and competence."));
        }

        if (!HasOperationalInvestmentContent(monthlyTarget, weeks))
        {
            return Result.Failure(
                Error.Validation(
                    "trafficinvestments.content_required",
                    "Investment must have a positive monthly target or at least one positive deposit or spend."));
        }

        return Result.Success();
    }

    private static bool HasOperationalInvestmentContent(
        decimal monthlyTarget,
        IReadOnlyList<TrafficWeekRequest> weeks)
    {
        if (monthlyTarget > 0m)
        {
            return true;
        }

        foreach (var week in weeks)
        {
            foreach (var deposit in week.Deposits)
            {
                if (deposit.RequestedAmount > 0m || deposit.DepositedAmount > 0m)
                {
                    return true;
                }
            }

            foreach (var spend in week.ChannelSpends)
            {
                if (spend.Amount > 0m)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Result ValidateInvestmentListFilters(TrafficInvestmentListFilters filters)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("trafficinvestments.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private static Result ValidateProjectDepositListFilters(TrafficProjectDepositListFilters filters)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("trafficdeposits.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("trafficdeposits.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private static TrafficProjectDepositResponse MapProjectDeposit(TrafficProjectDeposit deposit) =>
        new(
            deposit.Id,
            deposit.ProjectId,
            deposit.Project.Name,
            deposit.DepositDate,
            deposit.Amount,
            deposit.Notes);

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void UpsertWeeks(TrafficInvestment investment, IReadOnlyList<TrafficWeekRequest> weeks)
    {
        foreach (var weekRequest in weeks.OrderBy(week => week.WeekNumber))
        {
            var week = investment.Weeks.FirstOrDefault(existing => existing.WeekNumber == weekRequest.WeekNumber);
            if (week is null)
            {
                investment.Weeks.Add(TrafficInvestmentMapper.CreateWeekEntity(investment.Id, weekRequest));
                continue;
            }

            ReplaceDeposits(week, weekRequest.Deposits);
            ReplaceChannelSpends(week, weekRequest.ChannelSpends);
        }
    }

    private void ReplaceDeposits(TrafficInvestmentWeek week, IReadOnlyList<TrafficWeekDepositRequest> deposits)
    {
        foreach (var existing in week.Deposits.ToList())
        {
            week.Deposits.Remove(existing);
        }

        foreach (var depositRequest in deposits)
        {
            week.Deposits.Add(new TrafficWeekDeposit
            {
                Id = Guid.NewGuid(),
                TrafficInvestmentWeekId = week.Id,
                RequestedAmount = depositRequest.RequestedAmount,
                DepositedAmount = depositRequest.DepositedAmount,
                Status = TrafficInvestmentMapper.ResolveDepositStatus(
                    depositRequest.RequestedAmount,
                    depositRequest.DepositedAmount)
            });
        }
    }

    private void ReplaceChannelSpends(
        TrafficInvestmentWeek week,
        IReadOnlyList<TrafficWeekChannelSpendRequest> channelSpends)
    {
        var requestedChannels = channelSpends
            .Where(spend => spend.Amount > 0m)
            .GroupBy(spend => spend.Channel)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));

        foreach (var existing in week.ChannelSpends.ToList())
        {
            if (!requestedChannels.ContainsKey(existing.Channel))
            {
                week.ChannelSpends.Remove(existing);
            }
        }

        foreach (var (channel, amount) in requestedChannels)
        {
            var existing = week.ChannelSpends.FirstOrDefault(spend => spend.Channel == channel);
            if (existing is null)
            {
                week.ChannelSpends.Add(new TrafficWeekChannelSpend
                {
                    Id = Guid.NewGuid(),
                    TrafficInvestmentWeekId = week.Id,
                    Channel = channel,
                    Amount = amount
                });
            }
            else
            {
                existing.Amount = amount;
            }
        }
    }
}
