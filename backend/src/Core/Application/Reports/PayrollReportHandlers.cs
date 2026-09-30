using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Reports;

public sealed class GetPayrollReportHandler(IPayrollReportStore store)
    : IRequestHandler<GetPayrollReportQuery, Result<PayrollReportResponse>>
{
    public Task<Result<PayrollReportResponse>> Handle(
        GetPayrollReportQuery request,
        CancellationToken cancellationToken) =>
        store.GetPayrollReportAsync(request.Filters, request.Access, cancellationToken);
}

public sealed class GetPayrollReportExportHandler(
    IPayrollReportStore store,
    IPayrollReportXlsxExporter exporter)
    : IRequestHandler<GetPayrollReportExportQuery, Result<PayrollReportExportResult>>
{
    public async Task<Result<PayrollReportExportResult>> Handle(
        GetPayrollReportExportQuery request,
        CancellationToken cancellationToken)
    {
        var reportResult = await store.GetPayrollReportAsync(
            request.Filters,
            request.Access,
            cancellationToken);

        if (reportResult.IsFailure)
        {
            return Result<PayrollReportExportResult>.Failure(reportResult.Error!);
        }

        return Result<PayrollReportExportResult>.Success(exporter.Export(reportResult.Value!));
    }
}
