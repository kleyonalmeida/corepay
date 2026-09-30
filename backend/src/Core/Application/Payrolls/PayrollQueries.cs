using BuildingBlocks.Results;
using Core.Domain;
using MediatR;

namespace Core.Application.Payrolls;

public sealed record GetPayrollsQuery(
    string? Search,
    int? Month,
    int? Year,
    PayrollStatus? Status,
    Guid? DepartmentId,
    PayrollAccessContext Access,
    int? Page = null,
    int? PageSize = null) : IRequest<Result<PayrollsListResponse>>;

public sealed record GetPayrollFormOptionsQuery(
    PayrollAccessContext Access) : IRequest<Result<PayrollFormOptionsResponse>>;

public sealed record GetPayrollByIdQuery(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record PreviewPayrollEntryCommand(
    Guid PayrollId,
    Guid EntryId,
    PreviewPayrollEntryRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollEntryPreviewResponse>>;
