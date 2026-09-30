namespace WebAPI.Tests.AnalystMetrics;

internal static class AnalystMetricsTestHelper
{
    public static object CreatePayload(
        Guid collaboratorId,
        Guid projectId,
        int month = 9,
        int year = 2026,
        int ftdTotal = 120,
        int cpaCount = 35) =>
        new
        {
            collaboratorId,
            projectId,
            month,
            year,
            ftdTotal,
            cpaCount
        };
}
