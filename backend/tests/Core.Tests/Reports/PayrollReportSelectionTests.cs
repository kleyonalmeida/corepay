using Core.Application.Reports;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.Reports;

public class PayrollReportSelectionTests
{
    [Fact]
    public void SelectWinningPayrolls_ShouldPreferPaidOverDraft()
    {
        var departmentId = Guid.NewGuid();
        var paid = CreatePayroll(departmentId, 3, 2026, PayrollStatus.Paid, 5_000m);
        var draft = CreatePayroll(departmentId, 3, 2026, PayrollStatus.Draft, 9_000m);

        var winners = PayrollReportSelection.SelectWinningPayrolls([paid, draft]).ToList();

        winners.Should().ContainSingle();
        winners[0].Status.Should().Be(PayrollStatus.Paid);
        winners[0].TotalAmount.Should().Be(5_000m);
    }

    [Theory]
    [InlineData(PayrollStatus.Paid, PayrollStatus.Approved)]
    [InlineData(PayrollStatus.Approved, PayrollStatus.PendingApproval)]
    [InlineData(PayrollStatus.PendingApproval, PayrollStatus.Rejected)]
    public void SelectWinningPayrolls_ShouldFollowStatusPriority(PayrollStatus winner, PayrollStatus loser)
    {
        var departmentId = Guid.NewGuid();
        var winningPayroll = CreatePayroll(departmentId, 4, 2026, winner, 1_000m);
        var losingPayroll = CreatePayroll(departmentId, 4, 2026, loser, 2_000m);

        var winners = PayrollReportSelection.SelectWinningPayrolls([winningPayroll, losingPayroll]).ToList();

        winners.Should().ContainSingle();
        winners[0].Status.Should().Be(winner);
    }

    [Fact]
    public void SelectWinningPayrolls_ShouldExcludeDraftWithoutApprovedEntries()
    {
        var payroll = CreatePayroll(Guid.NewGuid(), 5, 2026, PayrollStatus.Draft, 1_000m, isApproved: false);

        PayrollReportSelection.SelectWinningPayrolls([payroll]).Should().BeEmpty();
    }

    [Fact]
    public void SelectWinningPayrolls_ShouldIncludeDraftWithApprovedEntry()
    {
        var payroll = CreatePayroll(Guid.NewGuid(), 5, 2026, PayrollStatus.Draft, 1_000m, isApproved: true);

        PayrollReportSelection.SelectWinningPayrolls([payroll]).Should().ContainSingle();
    }

    private static Core.Domain.Payroll CreatePayroll(
        Guid departmentId,
        int month,
        int year,
        PayrollStatus status,
        decimal totalAmount,
        bool isApproved = true)
    {
        var payrollId = Guid.NewGuid();
        return new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = departmentId,
            Month = month,
            Year = year,
            Status = status,
            TotalAmount = totalAmount,
            Entries =
            [
                new PayrollCollaboratorEntry
                {
                    Id = Guid.NewGuid(),
                    PayrollId = payrollId,
                    CollaboratorId = Guid.NewGuid(),
                    CollaboratorName = "Colaborador",
                    DepartmentId = departmentId,
                    IsApproved = isApproved,
                    Payload = new PayrollCollaboratorEntryPayload
                    {
                        CalculatedResult = new PayrollEntryResult { TotalAmount = totalAmount }
                    }
                }
            ]
        };
    }
}
