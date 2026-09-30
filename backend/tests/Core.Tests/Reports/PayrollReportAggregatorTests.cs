using Core.Application.Reports;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.Reports;

public class PayrollReportAggregatorTests
{
    private static readonly Guid DepartmentA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DepartmentB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectX = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProjectY = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid CollaboratorOne = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid CollaboratorTwo = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    [Fact]
    public void Aggregate_ShouldComputeSummaryCardsAndMonthlyAverageOverTwelveMonths()
    {
        var lines = new List<PayrollReportAggregator.ReportLineItem>
        {
            CreateLine(DepartmentA, "Comercial", 1, 2026, CollaboratorOne, "Ana", 12_000m, ProjectX, 7_000m),
            CreateLine(DepartmentB, "Gerência", 2, 2026, CollaboratorTwo, "Bruno", 6_000m, ProjectY, 6_000m)
        };

        var projectNames = new Dictionary<Guid, string>
        {
            [ProjectX] = "Projeto X",
            [ProjectY] = "Projeto Y"
        };

        var result = PayrollReportAggregator.Aggregate(
            2026,
            lines,
            projectNames,
            new PayrollReportFilterOptionsResponse([], []));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.TotalYear.Should().Be(18_000m);
        result.Value.Summary.MonthlyAverage.Should().Be(1_500m);
        result.Value.Summary.TopDepartmentName.Should().Be("Comercial");
        result.Value.Summary.TopDepartmentAmount.Should().Be(12_000m);
        result.Value.Summary.TopProjectName.Should().Be("Projeto X");
        result.Value.Summary.TopProjectAmount.Should().Be(7_000m);
        result.Value.Summary.CollaboratorCount.Should().Be(2);
        result.Value.MonthlySeries.Should().HaveCount(12);
        result.Value.MonthlySeries.Single(point => point.Month == 1).Amount.Should().Be(12_000m);
        result.Value.MonthlySeries.Single(point => point.Month == 2).Amount.Should().Be(6_000m);
    }

    [Fact]
    public void BuildLineItems_ShouldFilterByProjectAmount()
    {
        var payroll = CreatePayroll(
            DepartmentA,
            "Comercial",
            3,
            2026,
            PayrollStatus.Approved,
            CollaboratorOne,
            "Ana",
            5_000m,
            [(ProjectX, 3_000m), (ProjectY, 2_000m)]);

        var result = PayrollReportAggregator.BuildLineItems([payroll], ProjectX);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value[0].Amount.Should().Be(3_000m);
    }

    [Fact]
    public void BuildLineItems_ShouldSkipEntriesWithoutApprovedFlagOnPendingApproval()
    {
        var payrollId = Guid.NewGuid();
        var payroll = new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = DepartmentA,
            Department = new Department { Id = DepartmentA, Name = "Comercial" },
            Month = 4,
            Year = 2026,
            Status = PayrollStatus.PendingApproval,
            Entries =
            [
                CreateEntry(payrollId, CollaboratorOne, "Ana", true, 2_000m, [(ProjectX, 2_000m)]),
                CreateEntry(payrollId, CollaboratorTwo, "Bruno", false, 9_000m, [(ProjectX, 9_000m)])
            ]
        };

        var result = PayrollReportAggregator.BuildLineItems([payroll], null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value[0].CollaboratorName.Should().Be("Ana");
    }

    private static PayrollReportAggregator.ReportLineItem CreateLine(
        Guid departmentId,
        string departmentName,
        int month,
        int year,
        Guid collaboratorId,
        string collaboratorName,
        decimal amount,
        Guid projectId,
        decimal projectAmount) =>
        new(
            departmentId,
            departmentName,
            month,
            year,
            collaboratorId,
            collaboratorName,
            amount,
            new Dictionary<Guid, decimal> { [projectId] = projectAmount });

    private static Core.Domain.Payroll CreatePayroll(
        Guid departmentId,
        string departmentName,
        int month,
        int year,
        PayrollStatus status,
        Guid collaboratorId,
        string collaboratorName,
        decimal totalAmount,
        (Guid ProjectId, decimal Amount)[] projectTotals)
    {
        var payrollId = Guid.NewGuid();
        return new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = departmentId,
            Department = new Department { Id = departmentId, Name = departmentName },
            Month = month,
            Year = year,
            Status = status,
            TotalAmount = totalAmount,
            Entries = [CreateEntry(payrollId, collaboratorId, collaboratorName, true, totalAmount, projectTotals)]
        };
    }

    private static PayrollCollaboratorEntry CreateEntry(
        Guid payrollId,
        Guid collaboratorId,
        string collaboratorName,
        bool isApproved,
        decimal totalAmount,
        (Guid ProjectId, decimal Amount)[] projectTotals) =>
        new()
        {
            Id = Guid.NewGuid(),
            PayrollId = payrollId,
            CollaboratorId = collaboratorId,
            CollaboratorName = collaboratorName,
            DepartmentId = DepartmentA,
            IsApproved = isApproved,
            Payload = new PayrollCollaboratorEntryPayload
            {
                CalculatedResult = new PayrollEntryResult { TotalAmount = totalAmount },
                DisplayProjectTotals = projectTotals
                    .Select(total => new ProjectTotalAllocation(total.ProjectId, total.Amount))
                    .ToList()
            }
        };
}
