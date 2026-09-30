using FluentAssertions;
using WebApp.Blazor.Components.Payroll;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollEditabilityGuardTests : BlazorComponentTestContext
{
    [Theory]
    [InlineData("draft")]
    [InlineData("rejected")]
    public async Task EditableStatus_ShouldRenderChildContent(string status)
    {
        var cut = Render<PayrollEditabilityGuard>(parameters => parameters
            .Add(p => p.PayrollId, Guid.NewGuid())
            .Add(p => p.Status, status)
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Formulário editável")));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Formulário editável");
            cut.Markup.Should().NotContain("Folha não editável");
        });
    }

    [Theory]
    [InlineData("pendingApproval", "aguardando aprovação")]
    [InlineData("approved", "já foi aprovada")]
    [InlineData("paid", "já foi paga")]
    public async Task NonEditableStatus_ShouldBlockForm(string status, string snippet)
    {
        var payrollId = Guid.NewGuid();
        var cut = Render<PayrollEditabilityGuard>(parameters => parameters
            .Add(p => p.PayrollId, payrollId)
            .Add(p => p.Status, status)
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Formulário editável")));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Folha não editável");
            cut.Markup.Should().Contain(snippet);
            cut.Markup.Should().NotContain("Formulário editável");
            cut.Markup.Should().Contain($"/payrolls/{payrollId}");
            cut.Markup.Should().Contain("payroll-form__readonly-card");
            cut.Markup.Should().Contain("status-badge");
        });
    }
}
