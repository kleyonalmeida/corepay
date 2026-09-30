using Core.Application.Reports;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.Reports;

public class PayrollReportSnapshotReaderTests
{
    [Fact]
    public void GetEntryTotalAmount_ShouldReturnLockedTotal()
    {
        var entry = new PayrollCollaboratorEntry
        {
            Payload = new PayrollCollaboratorEntryPayload
            {
                CalculatedResult = new PayrollEntryResult
                {
                    TotalAmount = 4_500m,
                    PlatformTotal = 300m
                }
            }
        };

        var result = PayrollReportSnapshotReader.GetEntryTotalAmount(entry);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(4_500m);
    }

    [Fact]
    public void GetEntryTotalAmount_ShouldFailWhenSnapshotMissing()
    {
        var entry = new PayrollCollaboratorEntry
        {
            Payload = new PayrollCollaboratorEntryPayload()
        };

        var result = PayrollReportSnapshotReader.GetEntryTotalAmount(entry);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("reports.snapshot_missing");
    }

    [Fact]
    public void GetProjectAmount_ShouldSumDisplayProjectTotals()
    {
        var projectId = Guid.NewGuid();
        var entry = new PayrollCollaboratorEntry
        {
            Payload = new PayrollCollaboratorEntryPayload
            {
                DisplayProjectTotals =
                [
                    new ProjectTotalAllocation(projectId, 1_200m),
                    new ProjectTotalAllocation(projectId, 800m),
                    new ProjectTotalAllocation(Guid.NewGuid(), 500m)
                ]
            }
        };

        PayrollReportSnapshotReader.GetProjectAmount(entry, projectId).Should().Be(2_000m);
    }
}
