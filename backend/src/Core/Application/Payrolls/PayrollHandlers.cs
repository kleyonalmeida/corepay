using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Payrolls;

public sealed class GetPayrollsHandler(IPayrollStore store)
    : IRequestHandler<GetPayrollsQuery, Result<PayrollsListResponse>>
{
    public Task<Result<PayrollsListResponse>> Handle(
        GetPayrollsQuery request,
        CancellationToken cancellationToken) =>
        store.GetPayrollsAsync(
            new PayrollListFilters(
                request.Search,
                request.Month,
                request.Year,
                request.Status,
                request.DepartmentId,
                request.Page,
                request.PageSize),
            request.Access,
            cancellationToken);
}

public sealed class GetPayrollFormOptionsHandler(IPayrollStore store)
    : IRequestHandler<GetPayrollFormOptionsQuery, Result<PayrollFormOptionsResponse>>
{
    public Task<Result<PayrollFormOptionsResponse>> Handle(
        GetPayrollFormOptionsQuery request,
        CancellationToken cancellationToken) =>
        store.GetFormOptionsAsync(request.Access, cancellationToken);
}

public sealed class GetPayrollByIdHandler(IPayrollStore store)
    : IRequestHandler<GetPayrollByIdQuery, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        GetPayrollByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetDetailByIdAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class CreatePayrollHandler(IPayrollStore store)
    : IRequestHandler<CreatePayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        CreatePayrollCommand request,
        CancellationToken cancellationToken) =>
        store.CreatePayrollAsync(request.Request, request.Access, cancellationToken);
}

public sealed class UpdatePayrollHandler(IPayrollStore store)
    : IRequestHandler<UpdatePayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        UpdatePayrollCommand request,
        CancellationToken cancellationToken) =>
        store.UpdatePayrollShellAsync(request.PayrollId, request.Request, request.Access, cancellationToken);
}

public sealed class DuplicatePayrollHandler(IPayrollStore store)
    : IRequestHandler<DuplicatePayrollCommand, Result<PayrollSummaryResponse>>
{
    public Task<Result<PayrollSummaryResponse>> Handle(
        DuplicatePayrollCommand request,
        CancellationToken cancellationToken) =>
        store.DuplicateToNextMonthAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class PreviewPayrollEntryHandler(IPayrollStore store)
    : IRequestHandler<PreviewPayrollEntryCommand, Result<PayrollEntryPreviewResponse>>
{
    public Task<Result<PayrollEntryPreviewResponse>> Handle(
        PreviewPayrollEntryCommand request,
        CancellationToken cancellationToken) =>
        store.PreviewEntryAsync(
            request.PayrollId,
            request.EntryId,
            request.Request,
            request.Access,
            cancellationToken);
}

public sealed class SubmitPayrollHandler(IPayrollStore store)
    : IRequestHandler<SubmitPayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        SubmitPayrollCommand request,
        CancellationToken cancellationToken) =>
        store.SubmitPayrollAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class ApprovePayrollHandler(IPayrollStore store)
    : IRequestHandler<ApprovePayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        ApprovePayrollCommand request,
        CancellationToken cancellationToken) =>
        store.ApprovePayrollAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class RejectPayrollHandler(IPayrollStore store)
    : IRequestHandler<RejectPayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        RejectPayrollCommand request,
        CancellationToken cancellationToken) =>
        store.RejectPayrollAsync(request.PayrollId, request.Request, request.Access, cancellationToken);
}

public sealed class RecalculatePayrollHandler(IPayrollStore store)
    : IRequestHandler<RecalculatePayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        RecalculatePayrollCommand request,
        CancellationToken cancellationToken) =>
        store.RecalculatePayrollAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class DeletePayrollHandler(IPayrollStore store)
    : IRequestHandler<DeletePayrollCommand, Result>
{
    public Task<Result> Handle(DeletePayrollCommand request, CancellationToken cancellationToken) =>
        store.DeletePayrollAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class PayPayrollHandler(IPayrollStore store)
    : IRequestHandler<PayPayrollCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        PayPayrollCommand request,
        CancellationToken cancellationToken) =>
        store.PayPayrollAsync(request.PayrollId, request.Access, cancellationToken);
}

public sealed class ApprovePayrollEntryHandler(IPayrollStore store)
    : IRequestHandler<ApprovePayrollEntryCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        ApprovePayrollEntryCommand request,
        CancellationToken cancellationToken) =>
        store.ApproveEntryAsync(request.PayrollId, request.EntryId, request.Access, cancellationToken);
}

public sealed class PayPayrollEntryHandler(IPayrollStore store)
    : IRequestHandler<PayPayrollEntryCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        PayPayrollEntryCommand request,
        CancellationToken cancellationToken) =>
        store.PayEntryAsync(
            request.PayrollId,
            request.EntryId,
            request.Request,
            request.Access,
            cancellationToken);
}

public sealed class SetPayrollEntryNfHandler(IPayrollStore store)
    : IRequestHandler<SetPayrollEntryNfCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        SetPayrollEntryNfCommand request,
        CancellationToken cancellationToken) =>
        store.SetEntryNfAsync(
            request.PayrollId,
            request.EntryId,
            request.Request,
            request.Access,
            cancellationToken);
}

public sealed class UpdatePayrollEntryHandler(IPayrollStore store)
    : IRequestHandler<UpdatePayrollEntryCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        UpdatePayrollEntryCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateEntryAsync(
            request.PayrollId,
            request.EntryId,
            request.Request,
            request.Access,
            cancellationToken);
}

public sealed class AddCollaboratorEntryHandler(IPayrollStore store)
    : IRequestHandler<AddCollaboratorEntryCommand, Result<PayrollDetailResponse>>
{
    public Task<Result<PayrollDetailResponse>> Handle(
        AddCollaboratorEntryCommand request,
        CancellationToken cancellationToken) =>
        store.AddCollaboratorEntryAsync(
            request.PayrollId,
            request.Request,
            request.Access,
            cancellationToken);
}
