using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Collaborators;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Collaborators;

public class CollaboratorFormDialogTests : BlazorComponentTestContext
{
    [Fact]
    public void CollaboratorFormDialog_InactiveWithoutDismissal_ShowsLocalError()
    {
        var cut = Render<CollaboratorFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Departments, CreateDepartments())
            .Add(p => p.CareerLevels, CreateCareerLevels()));

        cut.Find("input[placeholder='Nome completo']").Input("Inativo Teste");
        cut.FindAll("select")[0].Change(CreateDepartments()[0].Id.ToString());
        cut.Find("button[aria-label='Ativo']").Click();
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Informe a data de demissão para inativar o colaborador.");
        });
    }

    [Fact]
    public void CollaboratorFormDialog_ValidSubmit_InvokesOnSubmitWithRequest()
    {
        CollaboratorRequest? capturedRequest = null;
        var cut = Render<CollaboratorFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Departments, CreateDepartments())
            .Add(p => p.CareerLevels, CreateCareerLevels())
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<CollaboratorRequest>(this, request =>
            {
                capturedRequest = request;
                return Task.CompletedTask;
            })));

        cut.Find("input[placeholder='Nome completo']").Input("Carla Comercial");
        cut.FindAll("select")[0].Change(CreateDepartments()[0].Id.ToString());
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Name.Should().Be("Carla Comercial");
            capturedRequest.IsActive.Should().BeTrue();
        });
    }

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

    private static IReadOnlyList<CareerLevelDto> CreateCareerLevels() =>
    [
        new CareerLevelDto(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Analista Comercial Júnior",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CalculationProfile.CommercialAnalyst,
            true,
            1500m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            2m,
            2.5m,
            3m,
            5m,
            250,
            350m,
            4m,
            5m,
            6m,
            20_000m,
            250m,
            1m,
            200m,
            70m,
            4m,
            5m,
            0.3m,
            0.5m,
            0.5m,
            0.8m,
            10m,
            50m,
            1.2m,
            1.5m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m)
    ];
}
