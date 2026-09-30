using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Cashflow;

public sealed class GetCashflowEntriesHandler(ICashflowStore store)
    : IRequestHandler<GetCashflowEntriesQuery, Result<CashflowListResponse>>
{
    public Task<Result<CashflowListResponse>> Handle(
        GetCashflowEntriesQuery request,
        CancellationToken cancellationToken) =>
        store.GetListAsync(request.Filters, cancellationToken);
}

public sealed class GetCashflowEntryByIdHandler(ICashflowStore store)
    : IRequestHandler<GetCashflowEntryByIdQuery, Result<CashflowEntryResponse>>
{
    public Task<Result<CashflowEntryResponse>> Handle(
        GetCashflowEntryByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetByIdAsync(request.EntryId, cancellationToken);
}

public sealed class GetCashflowInstallmentGroupHandler(ICashflowStore store)
    : IRequestHandler<GetCashflowInstallmentGroupQuery, Result<IReadOnlyList<CashflowEntryResponse>>>
{
    public Task<Result<IReadOnlyList<CashflowEntryResponse>>> Handle(
        GetCashflowInstallmentGroupQuery request,
        CancellationToken cancellationToken) =>
        store.GetInstallmentGroupAsync(request.CompraId, cancellationToken);
}

public sealed class CreateCashflowEntryHandler(ICashflowStore store)
    : IRequestHandler<CreateCashflowEntryCommand, Result<CashflowCreateResponse>>
{
    public Task<Result<CashflowCreateResponse>> Handle(
        CreateCashflowEntryCommand request,
        CancellationToken cancellationToken) =>
        store.CreateAsync(request.Request, cancellationToken);
}

public sealed class UpdateCashflowEntryHandler(ICashflowStore store)
    : IRequestHandler<UpdateCashflowEntryCommand, Result<CashflowEntryResponse>>
{
    public Task<Result<CashflowEntryResponse>> Handle(
        UpdateCashflowEntryCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateAsync(request.EntryId, request.Request, cancellationToken);
}

public sealed class DeleteCashflowEntryHandler(ICashflowStore store)
    : IRequestHandler<DeleteCashflowEntryCommand, Result>
{
    public Task<Result> Handle(
        DeleteCashflowEntryCommand request,
        CancellationToken cancellationToken) =>
        store.DeleteAsync(request.EntryId, cancellationToken);
}

public sealed class GetCashflowReportHandler(ICashflowStore store)
    : IRequestHandler<GetCashflowReportQuery, Result<CashflowReportResponse>>
{
    public Task<Result<CashflowReportResponse>> Handle(
        GetCashflowReportQuery request,
        CancellationToken cancellationToken) =>
        store.GetReportAsync(request.Filters, cancellationToken);
}
