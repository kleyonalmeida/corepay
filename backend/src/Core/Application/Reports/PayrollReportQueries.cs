using BuildingBlocks.Results;
using Core.Application.Payrolls;
using MediatR;

namespace Core.Application.Reports;

public sealed record GetPayrollReportQuery(
    PayrollReportFilters Filters,
    PayrollAccessContext Access) : IRequest<Result<PayrollReportResponse>>;

public sealed record GetPayrollReportExportQuery(
    PayrollReportFilters Filters,
    PayrollAccessContext Access) : IRequest<Result<PayrollReportExportResult>>;
