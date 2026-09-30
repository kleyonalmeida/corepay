using FluentAssertions;
using WebApp.Blazor.Components.Ui;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests.Formatting;

public class PayrollStatusMapperTests
{
    [Theory]
    [InlineData("draft", StatusKind.Draft)]
    [InlineData("pendingApproval", StatusKind.PendingApproval)]
    [InlineData("approved", StatusKind.Approved)]
    [InlineData("rejected", StatusKind.Rejected)]
    [InlineData("paid", StatusKind.Paid)]
    public void ToStatusKind_ShouldMapKnownStatuses(string status, StatusKind expected) =>
        PayrollStatusMapper.ToStatusKind(status).Should().Be(expected);

    [Theory]
    [InlineData("draft", true)]
    [InlineData("rejected", true)]
    [InlineData("pendingApproval", false)]
    [InlineData("approved", false)]
    [InlineData("paid", false)]
    public void IsEditable_ShouldMatchWorkflowRules(string status, bool expected) =>
        PayrollStatusMapper.IsEditable(status).Should().Be(expected);
}
