using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Payroll;

public class PayrollWorkflowTransitionsTests
{
    [Theory]
    [InlineData(PayrollStatus.PendingApproval, true)]
    [InlineData(PayrollStatus.Draft, false)]
    [InlineData(PayrollStatus.Approved, false)]
    public void ValidateCanApprove_OnlyPendingApproval(PayrollStatus status, bool expectedSuccess)
    {
        var result = PayrollWorkflowTransitions.ValidateCanApprove(status);
        result.IsSuccess.Should().Be(expectedSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRejectionComment_EmptyIsInvalid(string? comment)
    {
        var result = PayrollWorkflowTransitions.ValidateRejectionComment(comment);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.rejection_comment_required");
    }

    [Fact]
    public void ValidateCanPay_BlocksPendingApproval()
    {
        var result = PayrollWorkflowTransitions.ValidateCanPay(PayrollStatus.PendingApproval);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payrolls.pending_approval_cannot_pay");
    }

    [Fact]
    public void ApprovePayroll_SetsMetadataAndApprovesAllEntries()
    {
        var payroll = CreatePayroll(PayrollStatus.PendingApproval, paid: false, approved: false);
        var approvedAt = new DateTimeOffset(2026, 3, 10, 15, 0, 0, TimeSpan.Zero);

        PayrollWorkflowTransitions.ApprovePayroll(payroll, "Director Test", approvedAt);

        payroll.Status.Should().Be(PayrollStatus.Approved);
        payroll.ApprovedBy.Should().Be("Director Test");
        payroll.ApprovedAt.Should().Be(approvedAt);
        payroll.RejectionComment.Should().BeNull();
        payroll.Entries.Should().OnlyContain(entry => entry.IsApproved);
    }

    [Fact]
    public void RejectPayroll_ClearsApprovalMetadata()
    {
        var payroll = CreatePayroll(PayrollStatus.PendingApproval, paid: false, approved: true);
        payroll.ApprovedBy = "Old Approver";
        payroll.ApprovedAt = DateTimeOffset.UtcNow;

        PayrollWorkflowTransitions.RejectPayroll(payroll, " Ajustar FTD ");

        payroll.Status.Should().Be(PayrollStatus.Rejected);
        payroll.RejectionComment.Should().Be("Ajustar FTD");
        payroll.ApprovedBy.Should().BeNull();
        payroll.ApprovedAt.Should().BeNull();
    }

    [Fact]
    public void SyncPayrollPaidStatus_AllPaidPromotesToPaid()
    {
        var payroll = CreatePayroll(PayrollStatus.Approved, paid: true, approved: true);

        PayrollWorkflowTransitions.SyncPayrollPaidStatus(payroll);

        payroll.Status.Should().Be(PayrollStatus.Paid);
    }

    [Fact]
    public void SyncPayrollPaidStatus_UnpayOneEntryDowngradesToApproved()
    {
        var payroll = CreatePayroll(PayrollStatus.Paid, paid: true, approved: true);
        payroll.Entries.Add(new PayrollCollaboratorEntry
        {
            Id = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            CollaboratorName = "Carlos",
            IsApproved = true,
            IsPaid = false
        });

        PayrollWorkflowTransitions.SyncPayrollPaidStatus(payroll);

        payroll.Status.Should().Be(PayrollStatus.Approved);
    }

    [Fact]
    public void SyncPayrollPaidStatus_EmptyPayrollDoesNotBecomePaid()
    {
        var payroll = new Core.Domain.Payroll
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Month = 3,
            Year = 2026,
            Status = PayrollStatus.Approved,
            Entries = []
        };

        PayrollWorkflowTransitions.SyncPayrollPaidStatus(payroll);

        payroll.Status.Should().Be(PayrollStatus.Approved);
    }

    private static Core.Domain.Payroll CreatePayroll(PayrollStatus status, bool paid, bool approved) =>
        new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Month = 3,
            Year = 2026,
            Status = status,
            Entries =
            [
                new PayrollCollaboratorEntry
                {
                    Id = Guid.NewGuid(),
                    CollaboratorId = Guid.NewGuid(),
                    CollaboratorName = "Ana",
                    IsApproved = approved,
                    IsPaid = paid
                }
            ]
        };
}
