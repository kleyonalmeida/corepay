using WebApp.Blazor.Components.Ui;

namespace WebApp.Blazor.Formatting;

public static class PayrollStatusMapper
{
    public static StatusKind ToStatusKind(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "draft" => StatusKind.Draft,
            "pendingapproval" => StatusKind.PendingApproval,
            "approved" => StatusKind.Approved,
            "rejected" => StatusKind.Rejected,
            "paid" => StatusKind.Paid,
            _ => StatusKind.Draft
        };

    public static bool IsEditable(string? status) =>
        status?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true
        || status?.Equals("rejected", StringComparison.OrdinalIgnoreCase) == true;
}
