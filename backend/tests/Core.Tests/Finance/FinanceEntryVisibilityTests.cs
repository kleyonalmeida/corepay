using Core.Application.Finance;
using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Finance;

public class FinanceEntryVisibilityTests
{
    [Theory]
    [InlineData(PayrollStatus.Approved, false, true)]
    [InlineData(PayrollStatus.Approved, true, true)]
    [InlineData(PayrollStatus.Paid, false, true)]
    [InlineData(PayrollStatus.Paid, true, true)]
    [InlineData(PayrollStatus.Draft, true, true)]
    [InlineData(PayrollStatus.Draft, false, false)]
    [InlineData(PayrollStatus.PendingApproval, true, true)]
    [InlineData(PayrollStatus.PendingApproval, false, false)]
    [InlineData(PayrollStatus.Rejected, true, true)]
    [InlineData(PayrollStatus.Rejected, false, false)]
    public void IsEntryVisible_ShouldMatchBusinessRule(
        PayrollStatus status,
        bool isApproved,
        bool expected)
    {
        FinanceEntryVisibility.IsEntryVisible(status, isApproved).Should().Be(expected);
    }

    [Theory]
    [InlineData(PayrollStatus.Approved, new[] { false, false }, true)]
    [InlineData(PayrollStatus.Paid, new[] { false }, true)]
    [InlineData(PayrollStatus.Draft, new[] { true, false }, true)]
    [InlineData(PayrollStatus.Draft, new[] { false, false }, false)]
    [InlineData(PayrollStatus.PendingApproval, new[] { false, true }, true)]
    [InlineData(PayrollStatus.Rejected, new[] { false, false }, false)]
    public void PayrollHasFinanceContent_ShouldMatchBusinessRule(
        PayrollStatus status,
        bool[] approvals,
        bool expected)
    {
        FinanceEntryVisibility.PayrollHasFinanceContent(status, approvals).Should().Be(expected);
    }
}
