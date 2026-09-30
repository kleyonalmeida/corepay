using Core.Domain;

namespace Core.Application.Notifications;

public static class PayrollNotificationContentBuilder
{
    public static (string Title, string Message) BuildSubmitted(Payroll payroll)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        return (
            "Folha submetida",
            $"{FormatDepartmentCompetence(payroll)} aguardando aprovação.");
    }

    public static (string Title, string Message) BuildApproved(Payroll payroll)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        return (
            "Folha aprovada",
            $"{FormatDepartmentCompetence(payroll)} foi aprovada.");
    }

    public static (string Title, string Message) BuildRejected(Payroll payroll)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        var comment = string.IsNullOrWhiteSpace(payroll.RejectionComment)
            ? "Sem comentário."
            : payroll.RejectionComment.Trim();

        return (
            "Folha reprovada",
            $"{FormatDepartmentCompetence(payroll)}: {comment}");
    }

    private static string FormatDepartmentCompetence(Payroll payroll)
    {
        var departmentName = payroll.Department?.Name ?? "Setor";
        return $"{departmentName} — {payroll.Month:D2}/{payroll.Year}";
    }
}
