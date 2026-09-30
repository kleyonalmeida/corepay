namespace Core.Application.Reports;

public sealed record PayrollReportExportResult(
    byte[] Content,
    string ContentType,
    string FileName);

public interface IPayrollReportXlsxExporter
{
    PayrollReportExportResult Export(PayrollReportResponse report);
}
