namespace WebAPI.Tests.Revenues;

internal static class ProjectRevenuesTestHelper
{
    public static object CreatePayload(
        Guid projectId,
        int month = 9,
        int year = 2026,
        decimal valueIgaming = 1000m,
        decimal valueVendas = 500m,
        decimal groupPercentage = 0m,
        string? notes = null) =>
        new
        {
            projectId,
            month,
            year,
            valueIgaming,
            valueVendas,
            groupPercentage,
            notes
        };

    public static object CreateUpdatePayload(
        Guid projectId,
        int month = 9,
        int year = 2026,
        decimal valueIgaming = 1000m,
        decimal valueVendas = 500m,
        decimal groupPercentage = 0m,
        string? notes = null) =>
        new
        {
            projectId,
            month,
            year,
            valueIgaming,
            valueVendas,
            groupPercentage,
            notes
        };
}
