using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Payrolls;

public sealed record DuplicatePayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollSummaryResponse>>;

public sealed record CreatePayrollCommand(
    CreatePayrollRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record UpdatePayrollCommand(
    Guid PayrollId,
    UpdatePayrollRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record SubmitPayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record ApprovePayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record RejectPayrollCommand(
    Guid PayrollId,
    RejectPayrollRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record RecalculatePayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record DeletePayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result>;

public sealed record PayPayrollCommand(
    Guid PayrollId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record ApprovePayrollEntryCommand(
    Guid PayrollId,
    Guid EntryId,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record PayPayrollEntryCommand(
    Guid PayrollId,
    Guid EntryId,
    SetEntryPaidRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record SetPayrollEntryNfCommand(
    Guid PayrollId,
    Guid EntryId,
    SetEntryNfRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record UpdatePayrollEntryCommand(
    Guid PayrollId,
    Guid EntryId,
    UpdatePayrollEntryRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;

public sealed record AddCollaboratorEntryCommand(
    Guid PayrollId,
    AddCollaboratorEntryRequest Request,
    PayrollAccessContext Access) : IRequest<Result<PayrollDetailResponse>>;
