using FluentAssertions;
using Microsoft.AspNetCore.Components;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Admin;
using WebApp.Blazor.Components.Cashflow;
using WebApp.Blazor.Components.ProjectRevenues;
using WebApp.Blazor.Components.Settings;
using WebApp.Blazor.Components.Traffic;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Validation;

public class CreationValidationFormTests : BlazorComponentTestContext
{
    [Fact]
    public void ProjectRevenueFormDialog_WithZeroValues_ShowsLocalError()
    {
        var cut = Render<ProjectRevenueFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Projects, CreateProjects())
            .Add(p => p.YearOptions, [2026])
            .Add(p => p.DefaultMonth, 9)
            .Add(p => p.DefaultYear, 2026));

        cut.FindAll("select")[0].Change(CreateProjects()[0].Id.ToString());
        cut.FindAll("button").First(button => button.TextContent?.Contains("Registrar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Informe ao menos um valor de iGaming ou Vendas maior que zero.");
        });
    }

    [Fact]
    public void TrafficInvestmentEditState_WithEmptyContent_RejectsRequest()
    {
        var state = new TrafficInvestmentEditState();
        state.ResetForCreate(defaultMonth: 9, defaultYear: 2026);
        state.ProjectId = Guid.NewGuid().ToString();

        var isValid = state.TryBuildRequest(out _, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("Informe uma meta positiva ou ao menos um depósito/gasto maior que zero.");
    }

    [Fact]
    public void PaymentMethodFormDialog_WithEmptyName_ShowsLocalError()
    {
        var cut = Render<PaymentMethodFormDialog>(parameters => parameters.Add(p => p.IsOpen, true));

        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("O nome é obrigatório.");
        });
    }

    [Fact]
    public void CashflowEntryFormDialog_SaidaWithoutPaymentMethod_ShowsLocalError()
    {
        var cut = Render<CashflowEntryFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.FilterOptions, new CashflowFilterOptionsDto([], [], []))
            .Add(p => p.YearOptions, [2026])
            .Add(p => p.DefaultMonth, 9)
            .Add(p => p.DefaultYear, 2026));

        cut.FindAll("button").First(button => button.TextContent?.Contains("Saída") == true).Click();
        cut.Find("input[type='date']").Input("2026-09-15");
        cut.Find(".ui-input--money").Input("100");

        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Selecione a forma de pagamento.");
        });
    }

    [Fact]
    public void UserFormDialog_ManagerWithoutDepartment_ShowsLocalError()
    {
        var cut = Render<UserFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.AvailableRoles, [new RoleDto("role-1", AppRoles.Manager, [])])
            .Add(p => p.AvailableDepartments, CreateDepartments()));

        cut.Find("input[placeholder='Nome de exibição']").Input("Gerente Teste");
        cut.Find("input[type='email']").Input("gerente@teste.com");
        cut.Find("input[placeholder='Mínimo 8 caracteres']").Input("Senha123!");
        cut.Find("input[type='checkbox']").Change(true);

        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Gerentes devem ter ao menos um setor selecionado.");
        });
    }

    private static IReadOnlyList<ProjectDto> CreateProjects() =>
    [
        new ProjectDto(
            Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7"),
            "Projeto Demo",
            null,
            ProjectPlatform.Lastlink,
            true,
            false,
            false,
            false)
    ];

    private static IReadOnlyList<DepartmentDto> CreateDepartments() =>
    [
        new DepartmentDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Analistas Comerciais",
            CalculationProfile.CommercialAnalyst,
            0m,
            200_000m,
            0.4m,
            null,
            true,
            false,
            false)
    ];
}
