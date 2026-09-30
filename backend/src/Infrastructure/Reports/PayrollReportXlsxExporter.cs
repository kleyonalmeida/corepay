using ClosedXML.Excel;
using Core.Application.Reports;

namespace Infrastructure.Reports;

public sealed class PayrollReportXlsxExporter : IPayrollReportXlsxExporter
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] MonthNames =
    [
        "Janeiro",
        "Fevereiro",
        "Março",
        "Abril",
        "Maio",
        "Junho",
        "Julho",
        "Agosto",
        "Setembro",
        "Outubro",
        "Novembro",
        "Dezembro"
    ];

    public PayrollReportExportResult Export(PayrollReportResponse report)
    {
        using var workbook = new XLWorkbook();
        BuildSummarySheet(workbook, report);
        BuildMonthlySheet(workbook, report);
        BuildDepartmentSheet(workbook, report);
        BuildProjectSheet(workbook, report);
        BuildCollaboratorSheet(workbook, report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new PayrollReportExportResult(
            stream.ToArray(),
            ContentType,
            $"relatorio-folha-{report.Year}.xlsx");
    }

    private static void BuildSummarySheet(XLWorkbook workbook, PayrollReportResponse report)
    {
        var sheet = workbook.Worksheets.Add("Resumo");
        var summary = report.Summary;

        sheet.Cell(1, 1).Value = "Ano";
        sheet.Cell(1, 2).Value = report.Year;

        sheet.Cell(2, 1).Value = "Total do ano";
        WriteMoneyCell(sheet.Cell(2, 2), summary.TotalYear);

        sheet.Cell(3, 1).Value = "Média mensal";
        WriteMoneyCell(sheet.Cell(3, 2), summary.MonthlyAverage);

        sheet.Cell(4, 1).Value = "Maior setor";
        sheet.Cell(4, 2).Value = summary.TopDepartmentName ?? "-";
        WriteMoneyCell(sheet.Cell(4, 3), summary.TopDepartmentAmount);

        sheet.Cell(5, 1).Value = "Maior projeto";
        sheet.Cell(5, 2).Value = summary.TopProjectName ?? "-";
        WriteMoneyCell(sheet.Cell(5, 3), summary.TopProjectAmount);

        sheet.Cell(6, 1).Value = "Colaboradores";
        sheet.Cell(6, 2).Value = summary.CollaboratorCount;

        sheet.Columns().AdjustToContents();
        sheet.Column(1).Style.Font.Bold = true;
    }

    private static void BuildMonthlySheet(XLWorkbook workbook, PayrollReportResponse report)
    {
        var sheet = workbook.Worksheets.Add("Mensal");
        WriteHeaderRow(sheet, "Mês", "Total");

        var row = 2;
        foreach (var point in report.MonthlySeries.OrderBy(point => point.Month))
        {
            sheet.Cell(row, 1).Value = MonthNames[point.Month - 1];
            WriteMoneyCell(sheet.Cell(row, 2), point.Amount);
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildDepartmentSheet(XLWorkbook workbook, PayrollReportResponse report)
    {
        var sheet = workbook.Worksheets.Add("Por setor");
        WriteHeaderRow(sheet, "Setor", "Total", "Linhas", "Folhas de pagamento");

        var row = 2;
        foreach (var department in report.ByDepartment)
        {
            sheet.Cell(row, 1).Value = department.DepartmentName;
            WriteMoneyCell(sheet.Cell(row, 2), department.Amount);
            sheet.Cell(row, 3).Value = department.EntryCount;
            sheet.Cell(row, 4).Value = department.PayrollCount;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildProjectSheet(XLWorkbook workbook, PayrollReportResponse report)
    {
        var sheet = workbook.Worksheets.Add("Por projeto");
        WriteHeaderRow(sheet, "Projeto", "Total", "Linhas");

        var row = 2;
        foreach (var project in report.ByProject)
        {
            sheet.Cell(row, 1).Value = project.ProjectName;
            WriteMoneyCell(sheet.Cell(row, 2), project.Amount);
            sheet.Cell(row, 3).Value = project.EntryCount;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildCollaboratorSheet(XLWorkbook workbook, PayrollReportResponse report)
    {
        var sheet = workbook.Worksheets.Add("Por colaborador");
        WriteHeaderRow(sheet, "Colaborador", "Setor", "Total", "Competências");

        var row = 2;
        foreach (var collaborator in report.ByCollaborator)
        {
            sheet.Cell(row, 1).Value = collaborator.CollaboratorName;
            sheet.Cell(row, 2).Value = collaborator.DepartmentName;
            WriteMoneyCell(sheet.Cell(row, 3), collaborator.Amount);
            sheet.Cell(row, 4).Value = collaborator.CompetenceCount;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteHeaderRow(IXLWorksheet sheet, params string[] headers)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            var cell = sheet.Cell(1, column + 1);
            cell.Value = headers[column];
            cell.Style.Font.Bold = true;
        }
    }

    private static void WriteMoneyCell(IXLCell cell, decimal amount)
    {
        cell.Value = amount;
        cell.Style.NumberFormat.Format = "#,##0.00";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
}
