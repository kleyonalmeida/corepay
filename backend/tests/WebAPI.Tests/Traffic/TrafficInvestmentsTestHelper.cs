using Core.Domain.TrafficInvestmentCalculation;

namespace WebAPI.Tests.Traffic;

internal static class TrafficInvestmentsTestHelper
{
    public static object CreatePayload(
        Guid projectId,
        int month = 9,
        int year = 2026,
        decimal monthlyTarget = 4000m,
        IReadOnlyList<object>? weeks = null) =>
        new
        {
            projectId,
            month,
            year,
            monthlyTarget,
            weeks = weeks ??
            [
                new
                {
                    weekNumber = 1,
                    deposits = Array.Empty<object>(),
                    channelSpends = new[]
                    {
                        new { channel = TrafficMediaChannel.Telegram.ToString(), amount = 1000m }
                    }
                }
            ]
        };

    public static object CreateUpdatePayload(
        Guid projectId,
        int month = 9,
        int year = 2026,
        decimal monthlyTarget = 4000m,
        IReadOnlyList<object>? weeks = null) =>
        new
        {
            projectId,
            month,
            year,
            monthlyTarget,
            weeks = weeks ??
            [
                new
                {
                    weekNumber = 1,
                    deposits = Array.Empty<object>(),
                    channelSpends = new[]
                    {
                        new { channel = TrafficMediaChannel.Telegram.ToString(), amount = 1000m }
                    }
                }
            ]
        };

    public static object CreateProjectDepositPayload(
        Guid projectId,
        string depositDate = "2026-09-15",
        decimal amount = 500m,
        string? notes = null) =>
        new
        {
            projectId,
            depositDate,
            amount,
            notes
        };
}
