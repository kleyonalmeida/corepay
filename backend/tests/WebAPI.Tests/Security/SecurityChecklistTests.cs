using FluentAssertions;
using System.Reflection;
using Xunit.Abstractions;

namespace WebAPI.Tests.Security;

/// <summary>
/// Checklist §15.5 do roadmap — critérios de segurança rastreáveis (Fase 15.5).
/// Evidências concretas permanecem nos testes especializados referenciados abaixo.
/// </summary>
[Trait("Category", "Security")]
public sealed class SecurityChecklistTests
{
    private readonly ITestOutputHelper _output;

    public SecurityChecklistTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Checklist_WebhookMustNotLeakSecret()
    {
        AssertTestClassExists(typeof(Cashflow.FacilitiesWebhookEndpointTests));
        AssertTestMethodExists(
            typeof(Cashflow.FacilitiesWebhookEndpointTests),
            nameof(Cashflow.FacilitiesWebhookEndpointTests.Post_InvalidSignature_ShouldReturnUnauthorizedWithoutBodyOrSecret));
        AssertTestMethodExists(
            typeof(Auth.FacilitiesOptionsValidatorTests),
            nameof(Auth.FacilitiesOptionsValidatorTests.Validate_MissingSecret_ShouldFail));
    }

    [Fact]
    public void Checklist_ManagerIsolationMustReturn403OnCrossDepartmentAccess()
    {
        AssertTestClassExists(typeof(Payroll.PayrollsEndpointTests));
        AssertTestMethodExists(
            typeof(Payroll.PayrollsEndpointTests),
            nameof(Payroll.PayrollsEndpointTests.Manager_WithSingleDepartment_ShouldOnlySeeOwnPayrolls));
        AssertTestMethodExists(
            typeof(Collaborators.CollaboratorsEndpointTests),
            nameof(Collaborators.CollaboratorsEndpointTests.Manager_WithSingleDepartment_ShouldOnlySeeOwnCollaborators));
        AssertTestMethodExists(
            typeof(Dashboard.DashboardEndpointTests),
            nameof(Dashboard.DashboardEndpointTests.GetDashboard_Manager_ShouldOnlySeeAssignedDepartments));
        AssertTestMethodExists(
            typeof(Reports.PayrollReportEndpointTests),
            nameof(Reports.PayrollReportEndpointTests.GetPayrollReport_ManagerWithReportsPermission_ShouldRespectDepartmentScope));
    }

    [Fact]
    public void Checklist_FinancialMustNotApproveAndDirectorMustNotPay()
    {
        AssertTestMethodExists(
            typeof(Payroll.PayrollWorkflowEndpointTests),
            nameof(Payroll.PayrollWorkflowEndpointTests.FinancialUser_CannotApprovePendingPayroll));
        AssertTestMethodExists(
            typeof(Payroll.PayrollWorkflowEndpointTests),
            nameof(Payroll.PayrollWorkflowEndpointTests.FinancialUser_CannotRejectPendingPayroll));
        AssertTestMethodExists(
            typeof(Payroll.PayrollWorkflowEndpointTests),
            nameof(Payroll.PayrollWorkflowEndpointTests.DirectorUser_CannotPayApprovedPayroll));
        AssertTestMethodExists(
            typeof(Payroll.PayrollsEndpointTests),
            nameof(Payroll.PayrollsEndpointTests.GetPayrollById_FinancialApproved_CanPayButNotApprove));
        AssertTestMethodExists(
            typeof(Payroll.PayrollsEndpointTests),
            nameof(Payroll.PayrollsEndpointTests.GetPayrollById_DirectorApproved_CanApproveEntryButNotPay));
    }

    [Fact]
    public void Checklist_SensitiveValuesMustNotAppearInAuditLogs()
    {
        AssertTestMethodExists(
            typeof(ManagerDepartmentAccessAuditMiddlewareTests),
            nameof(ManagerDepartmentAccessAuditMiddlewareTests.ManagerCrossDepartmentAccess_ShouldEmitAuditLogWithoutSensitiveData));
    }

    [Fact]
    public void Checklist_JwtAndFacilitiesSecretMustBeConfigurationOnly()
    {
        AssertTestClassExists(typeof(Auth.JwtOptionsTests));
        AssertTestClassExists(typeof(Auth.FacilitiesOptionsValidatorTests));
        AssertNoHardcodedJwtKeyInAppsettings();
    }

    private static void AssertTestClassExists(Type testClass)
    {
        testClass.Assembly.GetType(testClass.FullName!, throwOnError: true).Should().NotBeNull();
    }

    private static void AssertTestMethodExists(Type testClass, string methodName)
    {
        testClass
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should()
            .NotBeNull($"expected security test {testClass.Name}.{methodName}");
    }

    private static void AssertNoHardcodedJwtKeyInAppsettings()
    {
        var repoRoot = FindRepoRoot();
        var appsettingsPath = Path.Combine(repoRoot, "backend", "src", "WebAPI", "appsettings.json");
        var developmentPath = Path.Combine(repoRoot, "backend", "src", "WebAPI", "appsettings.Development.json");

        File.Exists(appsettingsPath).Should().BeTrue();
        File.ReadAllText(appsettingsPath).Should().NotContain("\"Key\"");
        File.ReadAllText(appsettingsPath).Should().NotContain("WebhookSecret");
        File.ReadAllText(appsettingsPath).Should().NotContain("Password=");

        File.Exists(developmentPath).Should().BeTrue();
        File.ReadAllText(developmentPath).Should().NotContain("\"Key\"");
        File.ReadAllText(developmentPath).Should().NotContain("WebhookSecret");
        File.ReadAllText(developmentPath).Should().NotContain("Password=");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "agents.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
