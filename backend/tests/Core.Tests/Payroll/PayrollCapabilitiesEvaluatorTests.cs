using Core.Application.Payrolls;
using Core.Auth;
using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Payroll;

public class PayrollCapabilitiesEvaluatorTests
{
    private static PayrollAccessContext Context(
        IReadOnlyList<string> permissions,
        params string[] roles) =>
        new("user-1", roles.ToList(), null, "Test User", permissions);

    [Theory]
    [InlineData(PayrollStatus.PendingApproval)]
    [InlineData(PayrollStatus.Draft)]
    [InlineData(PayrollStatus.Approved)]
    public void Financial_CannotApproveOrReject(PayrollStatus status)
    {
        var financialPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Financial];
        var result = PayrollCapabilitiesEvaluator.Evaluate(status, Context(financialPermissions, AppRoles.Financial));

        result.ApprovePayroll.Should().BeFalse();
        result.RejectPayroll.Should().BeFalse();
        result.ApproveEntry.Should().BeFalse();
    }

    [Fact]
    public void Director_PendingApproval_CanApproveButNotPay()
    {
        var directorPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Director];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.PendingApproval,
            Context(directorPermissions, AppRoles.Director));

        result.ApprovePayroll.Should().BeTrue();
        result.RejectPayroll.Should().BeTrue();
        result.PayPayroll.Should().BeFalse();
        result.PayEntry.Should().BeFalse();
        result.ToggleNf.Should().BeFalse();
        result.Edit.Should().BeFalse();
    }

    [Theory]
    [InlineData(PayrollStatus.Draft)]
    [InlineData(PayrollStatus.Rejected)]
    public void Manager_EditableStatuses_CanEditAndRecalculate(PayrollStatus status)
    {
        var managerPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Manager];
        var result = PayrollCapabilitiesEvaluator.Evaluate(status, Context(managerPermissions, AppRoles.Manager));

        result.Edit.Should().BeTrue();
        result.Recalculate.Should().BeTrue();
        result.ApprovePayroll.Should().BeFalse();
        result.PayPayroll.Should().BeFalse();
    }

    [Fact]
    public void Manager_Approved_CannotEditOrPay()
    {
        var managerPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Manager];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Approved,
            Context(managerPermissions, AppRoles.Manager));

        result.Edit.Should().BeFalse();
        result.Recalculate.Should().BeFalse();
        result.PayPayroll.Should().BeFalse();
    }

    [Fact]
    public void Financial_Approved_CanPayButNotPostApprovalOnDraft()
    {
        var financialPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Financial];

        var approved = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Approved,
            Context(financialPermissions, AppRoles.Financial));
        approved.PayPayroll.Should().BeTrue();
        approved.PayEntry.Should().BeTrue();
        approved.ToggleNf.Should().BeTrue();
        approved.PostApprovalAdjustments.Should().BeTrue();

        var draft = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Draft,
            Context(financialPermissions, AppRoles.Financial));
        draft.PostApprovalAdjustments.Should().BeFalse();
    }

    [Fact]
    public void Financial_PendingApproval_CannotPay()
    {
        var financialPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Financial];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.PendingApproval,
            Context(financialPermissions, AppRoles.Financial));

        result.PayPayroll.Should().BeFalse();
        result.PayEntry.Should().BeFalse();
        result.ToggleNf.Should().BeFalse();
    }

    [Fact]
    public void Admin_PendingApproval_HasApproveAndNotPay()
    {
        var adminPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Admin];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.PendingApproval,
            Context(adminPermissions, AppRoles.Admin));

        result.ApprovePayroll.Should().BeTrue();
        result.RejectPayroll.Should().BeTrue();
        result.PayPayroll.Should().BeFalse();
        result.Delete.Should().BeTrue();
    }

    [Fact]
    public void Admin_Approved_HasPayAndDelete()
    {
        var adminPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Admin];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Approved,
            Context(adminPermissions, AppRoles.Admin));

        result.PayPayroll.Should().BeTrue();
        result.Delete.Should().BeTrue();
        result.ApproveEntry.Should().BeTrue();
    }

    [Fact]
    public void SuperAdmin_BypassesPermissionList()
    {
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.PendingApproval,
            Context([], AppRoles.SuperAdmin));

        result.ApprovePayroll.Should().BeTrue();
        result.PayPayroll.Should().BeFalse();
        result.Delete.Should().BeTrue();
    }

    [Fact]
    public void Director_Approved_CanApproveEntryButNotPay()
    {
        var directorPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Director];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Approved,
            Context(directorPermissions, AppRoles.Director));

        result.ApproveEntry.Should().BeTrue();
        result.PayPayroll.Should().BeFalse();
    }

    [Theory]
    [InlineData(PayrollStatus.Draft)]
    [InlineData(PayrollStatus.Rejected)]
    [InlineData(PayrollStatus.Approved)]
    [InlineData(PayrollStatus.Paid)]
    public void Financial_EditableStatuses_CanAddCollaborator(PayrollStatus status)
    {
        var financialPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Financial];
        var result = PayrollCapabilitiesEvaluator.Evaluate(status, Context(financialPermissions, AppRoles.Financial));

        result.AddCollaborator.Should().BeTrue();
    }

    [Fact]
    public void Financial_PendingApproval_CannotAddCollaborator()
    {
        var financialPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Financial];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.PendingApproval,
            Context(financialPermissions, AppRoles.Financial));

        result.AddCollaborator.Should().BeFalse();
    }

    [Fact]
    public void Director_Approved_CannotAddCollaborator()
    {
        var directorPermissions = RolePermissionMap.ReferenceRolePermissions[AppRoles.Director];
        var result = PayrollCapabilitiesEvaluator.Evaluate(
            PayrollStatus.Approved,
            Context(directorPermissions, AppRoles.Director));

        result.AddCollaborator.Should().BeFalse();
    }
}
